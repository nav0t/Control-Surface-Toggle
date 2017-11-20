using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ControlSurfaceToggle
{
    public class ModuleCSToggle : PartModule
    {

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        bool controlActive = true;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float authority; // value of the authority limiter if control surface is enabled

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
		bool wasInAtmo; // Used for checking whether  an atmosphere change happened when vessel unloaded, only used in start


		public ModuleControlSurface cs;

        bool setupCalled = false;
        static List<ModuleCSToggle> toggles;

        List<BaseField> csFields;

        static String debugTag = "[CSToggle] ";   

        public void setup() {
            //Debug.Log("setup called");
            cs = GetComponent<ModuleControlSurface>(); //get the parts control surface module
            
            if (toggles == null) { //if necessary, initialize static list
                toggles = new List<ModuleCSToggle>();
            }

			toggles.Add(this);

            removeNullToggles(); // Clean up elements from previous iterations of the list

            csFields = new List<BaseField>();

            if (cs.authorityLimiter != 0) { //We don't want to override authority if the surface is disabled
                authority = cs.authorityLimiter;
            }

            if (!controlActive) { //Needed to disable the appropriate gui event buttons
                disableCS();
			}else {
				enableCS();
			}

			bool inAtmo = FlightGlobals.ActiveVessel.atmDensity != 0;
			if (inAtmo && !wasInAtmo) { //If we changed atmosphere state while vessel was not loaded, enable / disable control surface
				enableCS();
			}
			if (!inAtmo && wasInAtmo) {
				disableCS();
			}

            setupCalled = true; //Setup done
		}

        public override void OnUpdate() {
			base.OnUpdate();

			if (!setupCalled) {
                setup();
            }
            
            

            wasInAtmo = FlightGlobals.ActiveVessel.atmDensity != 0; //updating wasInAtmo so it gets saved correctly
        }

        

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Activate Control Surface")]
        public void EventEnable() {
            foreach (Part p in part.symmetryCounterparts) {
                if (p.GetComponent<ModuleCSToggle>() != null) {
                    p.GetComponent<ModuleCSToggle>().enableCS();
                }
            }
            enableCS();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate Control Surface")]
        public void EventDisable() {
            foreach (Part p in part.symmetryCounterparts) {
                if (p.GetComponent<ModuleCSToggle>() != null) {
                    p.GetComponent<ModuleCSToggle>().disableCS();
                }
            }
            disableCS();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Activate All")]
        public void EventEnableAll() {
            enableGlobal(true);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate All")]
        public void EventDisableAll() {
            disableGlobal(true);
        }

        public static void enableGlobal(bool printMessage) {
            if (toggles == null) //List not initialized because we don't have any control surfaces, and haven't launched a vessel with control surfaces
                return;
            if (!vesselHasControlSurface())
                return;

			if (printMessage)
				ScreenMessages.PostScreenMessage("Enabled all Control Surfaces");

			foreach (ModuleCSToggle ct in toggles) {
                if (ct != null) {
                    ct.enableCS();
                }
            }
        }

        public static void disableGlobal(bool printMessage) {
			if (toggles == null)
				return;
			if (!vesselHasControlSurface())
				return;

			if (printMessage)
				ScreenMessages.PostScreenMessage("Disabled all Control Surfaces");

            foreach (ModuleCSToggle ct in toggles) {
                if (ct != null) {
                    ct.disableCS();
                }
            }
        }

        public static bool vesselHasControlSurface() {

            foreach(Part p in FlightGlobals.ActiveVessel.parts) { // check if any part of the active vessel is a control surface
                if (p.GetComponent<ModuleCSToggle>() != null || p.GetComponentInChildren<ModuleCSToggle>() != null) {
                    return true;
                }
            }
            return false;
        }

        public void toggleCS() {
            if (controlActive) {
                disableCS();
            }else {
                enableCS();
            }
        }

        public void setEventVisibility() {
         
            Events["EventEnable"].active = !controlActive;
            Events["EventEnableAll"].active = !controlActive;
            Events["EventDisable"].active = controlActive;
            Events["EventDisableAll"].active = controlActive;
        }


        public void enableCS() {
            //Debug.Log("enabled " + part.name);
            controlActive = true;
            setEventVisibility();

			cs.authorityLimiter = authority;
            for (int i = 0; i < csFields.Count; i++) { //Reactivate disabled fields
                csFields[i].guiActive = true;
            } 
        }

        public void disableCS() {
            //Debug.Log("disabled "+part.name);

            controlActive = false;
            setEventVisibility();

            if (cs.authorityLimiter != 0) { //avoid overwriting value if already disabled
                authority = cs.authorityLimiter;
            }
            cs.authorityLimiter = 0;


			for (int i = 0; i < cs.Fields.Count; i++) { // Save which fields have been disabled, so the correct one's will be enabled
				if (cs.Fields[i].guiActive) {
					cs.Fields[i].guiActive = false;
					if (!csFields.Contains(cs.Fields[i])) { //Avoid duplicates
						csFields.Add(cs.Fields[i]);
					}
				}
			}
        }

		static void removeNullToggles() {
			if (toggles == null) //Should technically never occur
				return;

			for (int i = toggles.Count - 1; i >= 0; i--) { //going from back to front removing instances that belonged to other vessels
				if (toggles[i] == null) {
					toggles.RemoveAt(i);
				}
			}
		}
	}
}

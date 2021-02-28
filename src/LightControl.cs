using SimpleJSON;
using System;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public Atom lightAtom;
        public FreeControllerV3 control;
        private AimConstrain aimConstrain;

        public void Init(Atom lightAtom, string lightType)
        {
            this.lightAtom = lightAtom;
            control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();

            // init defaults
            JSONStorable light = lightAtom.GetStorableByID("Light");
            light.SetStringChooserParamValue("type", lightType);

            control.onColor = new Color(1f, 1f, 1f, 0.5f);
            control.highlighted = false;
        }

        public void InitFromSave(Atom lightAtom, FreeControllerV3 target)
        {
            try
            {
                this.lightAtom = lightAtom;
                control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                control.onColor = new Color(1f, 1f, 1f, 0.5f);
                control.highlighted = false;

                if(target != null)
                {
                    control.physicsEnabled = true;
                    if(aimConstrain == null)
                    {
                        aimConstrain = lightAtom.gameObject.AddComponent<AimConstrain>();
                        aimConstrain.Init(control, target);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error($"Error initalizing from JSON for atom '{lightAtom?.name}': {e}", nameof(LightControl));
            }
        }

        public void OnSelectTarget()
        {
            if(aimConstrain == null)
            {
                aimConstrain = lightAtom.gameObject.AddComponent<AimConstrain>();
            }
            aimConstrain.targetCtrl = null;
            control.physicsEnabled = true;

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(target => aimConstrain.Init(control, target))
            );
        }

        public void OnStopPointing()
        {
            Destroy(aimConstrain);
            aimConstrain = null;
        }

        public string GetAimConstrainTargetName()
        {
            return aimConstrain?.targetCtrl?.name;
        }

        public JSONClass Serialize()
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = lightAtom.uid;
            if(aimConstrain?.targetCtrl != null)
            {
                json["aimingAtAtomUid"] = aimConstrain.targetCtrl.containingAtom.uid;
                json["aimingAtControl"] = aimConstrain.targetCtrl.name;
            }
            return json;
        }

        private void OnEnable()
        {
            if(aimConstrain != null)
            {
                aimConstrain.SetConstraintActive(true);
            }
        }

        private void OnDisable()
        {
            if(aimConstrain != null)
            {
                aimConstrain.SetConstraintActive(false);
            }
        }

        private void OnDestroy()
        {
            Destroy(lightAtom.GetComponentInChildren<AimConstrain>());
        }
    }
}

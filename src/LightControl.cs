using SimpleJSON;
using System;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public Atom lightAtom;
        private AimConstrain aimConstrain;

        public void Init(Atom lightAtom, string lightType)
        {
            this.lightAtom = lightAtom;

            // init defaults
            JSONStorable light = lightAtom.GetStorableByID("Light");
            light.SetStringChooserParamValue("type", lightType);

            FreeControllerV3 fc = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            fc.physicsEnabled = true;
            fc.onColor = new Color(1f, 1f, 1f, 0.5f);
            fc.highlighted = false;
        }

        public void InitFromSave(Atom lightAtom, FreeControllerV3 target)
        {
            try
            {
                this.lightAtom = lightAtom;

                if(target != null)
                {
                    if(aimConstrain == null)
                    {
                        aimConstrain = lightAtom.gameObject.AddComponent<AimConstrain>();
                        aimConstrain.Init(lightAtom);
                    }
                    aimConstrain.targetCtrl = target;
                    aimConstrain.AddAimConstraintTargetingTransform();
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
                aimConstrain.Init(lightAtom);
            }
            aimConstrain.targetCtrl = null;

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(target =>
                {
                    aimConstrain.targetCtrl = target;
                    aimConstrain.AddAimConstraintTargetingTransform();
                })
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

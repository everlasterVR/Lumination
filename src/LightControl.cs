using SimpleJSON;
using System;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public Atom lightAtom;
        private AimConstrain aimConstrain;

        public void Init(Atom lightAtom, LightType lightType)
        {
            this.lightAtom = lightAtom;

            // init defaults
            Light light = lightAtom.GetComponentInChildren<Light>();
            light.type = lightType;

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
                    aimConstrain.SetTarget(target);
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

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(target => aimConstrain.SetTarget(target))
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

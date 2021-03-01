using SimpleJSON;
using System;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public Atom lightAtom;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;
        public JSONStorableBool enableLookAt;

        public void Init(Atom lightAtom, string lightType)
        {
            this.lightAtom = lightAtom;
            control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            enableLookAt = new JSONStorableBool("Enable aiming at target", false);

            // init defaults
            JSONStorable light = lightAtom.GetStorableByID("Light");
            light.SetStringChooserParamValue("type", lightType);

            SetOnColor(UI.white);
        }

        public void InitFromSave(Atom lightAtom, FreeControllerV3 targetCtrl, bool enableLookAtVal)
        {
            try
            {
                this.lightAtom = lightAtom;
                control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                enableLookAt = new JSONStorableBool("Enable aiming at target", enableLookAtVal);

                control.onColor = new Color(1f, 1f, 1f, 0.5f);
                control.highlighted = false;

                if(targetCtrl != null)
                {
                    control.physicsEnabled = true;
                    target = targetCtrl;
                }
            }
            catch(Exception e)
            {
                Log.Error($"Error initalizing from JSON for atom '{lightAtom?.name}': {e}", nameof(LightControl));
            }
        }

        public void OnSelectTarget()
        {
            control.physicsEnabled = true;

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(targetCtrl => {
                    target = targetCtrl;
                    enableLookAt.val = true;
                })
            );
        }

        public void OnStopAiming()
        {
            enableLookAt.val = false;
            target = null;
        }

        public void SetOnColor(Color color)
        {
            control.onColor = color;
            control.highlighted = false; // trigger color change
        }

        private void FixedUpdate()
        {
            if(target == null || !enableLookAt.val)
            {
                return;
            }

            control.transform.LookAt(target.followWhenOff.position);
        }

        public string GetTargetString()
        {
            string uid = target?.containingAtom.uid;
            string name = target?.name;
            if(uid != null && name != null)
            {
                return $"{uid}:{name}";
            }
            return null;
        }

        public JSONClass Serialize()
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = lightAtom.uid;
            json["enableLookAt"].AsBool = enableLookAt.val;
            if(target != null)
            {
                json["aimingAtAtomUid"] = target.containingAtom.uid;
                json["aimingAtControl"] = target.name;
            }
            return json;
        }

        private void OnEnable()
        {
            if(enableLookAt != null)
            {
                enableLookAt.val = target != null;
            }
        }

        private void OnDisable()
        {
            if(enableLookAt != null)
            {
                enableLookAt.val = false;
            }
        }
    }
}

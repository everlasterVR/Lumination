using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public JSONStorable light;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;

        public JSONStorableColor lightColor;
        public JSONStorableBool enableLookAt;
        public JSONStorableBool autoIntensity;
        public JSONStorableBool autoRange;
        public JSONStorableBool autoSpotAngle;
        public JSONStorableFloat intensity;
        public JSONStorableFloat range;
        public JSONStorableStringChooser lightType;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat shadowStrength;

        public void Init(Atom lightAtom, string lightType)
        {
            light = lightAtom.GetStorableByID("Light");
            control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            SetOnColor(UI.lightGray);
            InitStorables();

            // init defaults
            light.SetStringChooserParamValue("type", lightType);
        }

        public void InitFromSave(Atom lightAtom, FreeControllerV3 targetCtrl, bool enableLookAtVal)
        {
            try
            {
                light = lightAtom.GetStorableByID("Light");
                control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                //TODO read from json
                InitStorables();
                enableLookAt = new JSONStorableBool("Enable aiming at target", enableLookAtVal);
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

        private void InitStorables(
            bool enableLookAtVal = false,
            bool autoIntensityVal = false,
            bool autoRangeVal = false,
            bool autoSpotAngleVal = false,
            string lightTypeVal = null
        )
        {
            lightColor = light.GetColorJSONParam("color");

            enableLookAt = new JSONStorableBool("Enable aiming at target", enableLookAtVal);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to target", autoIntensityVal);
            autoRange = new JSONStorableBool("Adjust range relative to target", autoRangeVal);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to target", autoSpotAngleVal);

            intensity = light.GetFloatJSONParam("intensity");
            range = light.GetFloatJSONParam("range");
            lightType = light.GetStringChooserJSONParam("type");
            lightType.choices = new List<string> { "Spot", "Point" };
            if(lightTypeVal != null)
            {
                lightType.val = lightTypeVal;
            }
            spotAngle = light.GetFloatJSONParam("spotAngle");
            shadowStrength = light.GetFloatJSONParam("shadowStrength");
        }

        public IEnumerator OnSelectTarget(Action<string> callback)
        {
            control.physicsEnabled = true;
            bool waiting = true;

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(targetCtrl => {
                    waiting = false;
                    target = targetCtrl;
                    enableLookAt.val = true;
                })
            );

            while(waiting)
            {
                yield return null;
            }

            callback(GetTargetString());
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

        public void OnStopAiming()
        {
            enableLookAt.val = false;
            target = null;
        }

        public void SetOnColor(Color color)
        {
            if(control == null)
            {
                return;
            }

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

        public JSONClass Serialize()
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = light.containingAtom.uid;
            //json["enableLookAt"].AsBool = enableLookAt.val;
            if(target != null)
            {
                json["aimingAtAtomUid"] = target.containingAtom.uid;
                json["aimingAtControl"] = target.name;
            }
            return json;
        }

        private void OnEnable()
        {
            SetOnColor(UI.lightGray);
            if(enableLookAt != null)
            {
                enableLookAt.val = target != null;
            }
        }

        private void OnDisable()
        {
            SetOnColor(UI.defaultOnColor);
            if(enableLookAt != null)
            {
                enableLookAt.val = false;
            }
        }
    }
}

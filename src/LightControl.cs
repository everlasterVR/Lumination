using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public JSONStorable light;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;

        public JSONStorableBool on;
        public JSONStorableColor lightColor;
        public JSONStorableBool enableLookAt;
        public JSONStorableBool autoIntensity;
        public JSONStorableBool autoRange;
        public JSONStorableBool autoSpotAngle;
        public JSONStorableStringChooser lightType;
        public JSONStorableFloat intensity;
        public JSONStorableFloat range;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat shadowStrength;

        public void Init(Atom lightAtom, string lightType)
        {
            light = lightAtom.GetStorableByID("Light");
            control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            SetOnColor(UI.lightGray);
            InitStorables(lightTypeVal: lightType);
        }

        public void InitFromJson(Atom lightAtom, JSONClass json)
        {
            try
            {
                light = lightAtom.GetStorableByID("Light");
                control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                InitStorables(
                    json["enableLookAt"].AsBool,
                    json["autoIntensity"].AsBool,
                    json["autoRange"].AsBool,
                    json["autoSpotAngle"].AsBool
                );

                FreeControllerV3 target = null;
                if(json["aimingAtAtomUid"] != null && json["aimingAtControl"] != null)
                {
                    string aimingAtAtomUid = json["aimingAtAtomUid"].Value;
                    string aimingAtControl = json["aimingAtControl"].Value;
                    Atom aimingAtAtom = SuperController.singleton.GetAtomByUid(aimingAtAtomUid);
                    target = aimingAtAtom?.gameObject
                        .GetComponentsInChildren<FreeControllerV3>()
                        .Where(it => it.name == aimingAtControl)
                        .FirstOrDefault();

                    if(target == null)
                    {
                        Log.Message($"Unable to point '{lightAtom.uid}' at atom " +
                            $"'{aimingAtAtomUid}' target control '{aimingAtControl}': " +
                            $"target mentioned in saved JSON but not found in scene.");
                    }
                    else
                    {
                        control.physicsEnabled = true;
                        this.target = target;
                    }
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
            on = light.GetBoolJSONParam("on");
            lightColor = Tools.CopyColorStorable(light.GetColorJSONParam("color"), true);
            enableLookAt = new JSONStorableBool("Enable aiming at Target", enableLookAtVal);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to Target", autoIntensityVal);
            autoRange = new JSONStorableBool("Adjust range relative to Target", autoRangeVal);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to Target", autoSpotAngleVal);

            lightType = new JSONStorableStringChooser(
                "type",
                new List<string> { "Spot", "Point" },
                lightTypeVal ?? light.GetStringChooserParamValue("type"),
                "Light Type",
                (val) => light.SetStringChooserParamValue("type", val)
            );
            if(lightTypeVal != null)
            {
                light.SetStringChooserParamValue("type", lightTypeVal);
            }

            intensity = Tools.CopyFloatStorable(light.GetFloatJSONParam("intensity"), true);
            range = Tools.CopyFloatStorable(light.GetFloatJSONParam("range"), true);
            spotAngle = Tools.CopyFloatStorable(light.GetFloatJSONParam("spotAngle"), true);
            shadowStrength = Tools.CopyFloatStorable(light.GetFloatJSONParam("shadowStrength"), true);
        }

        public IEnumerator OnSelectTarget(Action<string> callback)
        {
            control.physicsEnabled = true;
            bool waiting = true;

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(targetCtrl =>
                {
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
            json["enableLookAt"].AsBool = enableLookAt.val;
            json["autoIntensity"].AsBool = autoIntensity.val;
            json["autoRange"].AsBool = autoRange.val;
            json["autoSpotAngle"].AsBool = autoSpotAngle.val;
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

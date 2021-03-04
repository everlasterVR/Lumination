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
        public static readonly List<string> types = new List<string> { "Spot", "Point" };

        private Log log = new Log(nameof(LightControl));
        public JSONStorable light;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;

        public JSONStorableBool on;
        public JSONStorableColor lightColor;
        public JSONStorableBool enableLookAt;
        private bool activeEnableLookAtVal;
        public JSONStorableBool autoIntensity;
        public JSONStorableBool autoRange;
        public JSONStorableBool autoSpotAngle;
        private bool activeAutoSpotAngleVal;
        public JSONStorableFloat distanceFromTarget;

        public JSONStorableStringChooser lightType;
        public JSONStorableFloat intensity;
        public JSONStorableFloat range;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat shadowStrength;

        public string prevLightType;

        public void Init(Atom lightAtom, string lightType)
        {
            light = lightAtom.GetStorableByID("Light");
            control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            SetOnColor(UI.lightGray);
            activeEnableLookAtVal = lightType == "Spot";
            activeAutoSpotAngleVal = false;
            InitStorables(lightTypeVal: lightType);
        }

        public void InitFromJson(Atom lightAtom, JSONClass json)
        {
            try
            {
                light = lightAtom.GetStorableByID("Light");
                control = lightAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                SetOnColor(UI.lightGray);
                bool isSpotLight = light.GetStringChooserParamValue("type") == "Spot";
                activeEnableLookAtVal = isSpotLight && json["enableLookAt"].AsBool;
                activeAutoSpotAngleVal = isSpotLight && json["autoSpotAngle"].AsBool;
                InitStorables(
                    json["autoIntensity"].AsBool,
                    json["autoRange"].AsBool
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
                        log.Message($"Unable to point '{lightAtom.uid}' at atom " +
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
                log.Error($"Error initalizing from JSON for atom '{lightAtom?.name}': {e}");
            }
        }

        private void InitStorables(
            bool autoIntensityVal = false,
            bool autoRangeVal = false,
            string lightTypeVal = null
        )
        {
            on = light.GetBoolJSONParam("on");
            lightColor = Tools.CopyColorStorable(light.GetColorJSONParam("color"), true);
            enableLookAt = new JSONStorableBool("Enable aiming at Target", activeEnableLookAtVal);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to Target", autoIntensityVal);
            autoRange = new JSONStorableBool("Adjust range relative to Target", autoRangeVal);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to Target", activeAutoSpotAngleVal);
            distanceFromTarget = new JSONStorableFloat("Distance from Target", 1f, 0f, 25f);
            lightType = CreateLightTypeStorable(lightTypeVal);
            intensity = Tools.CopyFloatStorable(light.GetFloatJSONParam("intensity"), true);
            range = Tools.CopyFloatStorable(light.GetFloatJSONParam("range"), true);
            spotAngle = Tools.CopyFloatStorable(light.GetFloatJSONParam("spotAngle"), true);
            shadowStrength = Tools.CopyFloatStorable(light.GetFloatJSONParam("shadowStrength"), true);

            prevLightType = lightType.val;
        }

        private JSONStorableStringChooser CreateLightTypeStorable(string lightTypeVal)
        {
            JSONStorableStringChooser source = light.GetStringChooserJSONParam("type");
            JSONStorableStringChooser copy = new JSONStorableStringChooser(
                source.name,
                types, //exclude Directional and Area
                source.defaultVal,
                "Light Type",
                (val) => {
                    source.val = val; //update type if type changed in plugin UI
                    OnChooseLightType(val);
                }
            );
            copy.val = lightTypeVal ?? source.val;

            source.setJSONCallbackFunction = (jc) =>
            {
                if(!types.Contains(jc.val))
                {
                    log.Message($"'{jc.val}' type is not supported for lights controlled by this plugin.");
                    jc.val = "Point"; //default to Point light
                }
                copy.val = jc.val;
            };

            return copy;
        }

        private void OnChooseLightType(string val)
        {
            //uncheck enable aiming and adjust spot angle if Point light, restore actual values if Spot light
            if(val == "Spot")
            {
                enableLookAt.val = activeEnableLookAtVal;
                autoSpotAngle.val = activeAutoSpotAngleVal;
            }
            else if(val == "Point")
            {
                activeEnableLookAtVal = enableLookAt.val;
                enableLookAt.val = false;
                activeAutoSpotAngleVal = autoSpotAngle.val;
                autoSpotAngle.val = false;
            }
            else
            {
                throw new ArgumentException($"Invalid type {val}");
            }
        }

        public IEnumerator OnSelectTarget(Action<string> callback)
        {
            control.physicsEnabled = true;
            bool waiting = true;
            string currentTargetString = GetTargetString();

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(targetCtrl =>
                {
                    waiting = false;
                    target = targetCtrl;
                })
            );

            while(waiting)
            {
                yield return null;
            }

            string newTargetString = GetTargetString();
            callback(newTargetString == currentTargetString ? null : newTargetString);
        }

        public string GetTargetString()
        {
            if(target == null)
            {
                return null;
            }

            return $"{target.containingAtom.uid}:{target.name}";
        }

        public string GetButtonLabelTargetString()
        {
            if(target == null)
            {
                return null;
            }

            Atom atom = target.containingAtom;
            if(target.name == "control")
            {
                return UI.Capitalize(atom.uid);
            }

            return UI.Capitalize(target.name.Replace("Control", ""));
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

        public void SetInteractableElements()
        {
            //interactables
            UpdateInteractableByAutoIntensity(autoIntensity.val);
            UpdateInteractableByAutoRange(autoRange.val);
            UpdateInteractableByAutoSpotAngle(autoSpotAngle.val);
            UpdateInteractableByType(lightType.val);
        }

        public void AddInteractableListeners()
        {
            autoIntensity.toggle.onValueChanged.AddListener(UpdateInteractableByAutoIntensity);
            autoRange.toggle.onValueChanged.AddListener(UpdateInteractableByAutoRange);
            autoSpotAngle.toggle.onValueChanged.AddListener(UpdateInteractableByAutoSpotAngle);
            lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange(UpdateInteractableByType);
        }

        private void UpdateInteractableByAutoIntensity(bool val)
        {
            intensity.slider.interactable = !val;
        }

        private void UpdateInteractableByAutoRange(bool val)
        {
            range.slider.interactable = !val;
        }

        private void UpdateInteractableByAutoSpotAngle(bool val)
        {
            spotAngle.slider.interactable = !val && lightType.val == "Spot";
        }

        private void UpdateInteractableByType(string val)
        {
            bool isSpot = val == "Spot";
            enableLookAt.toggle.interactable = isSpot;
            autoSpotAngle.toggle.interactable = isSpot;
            spotAngle.slider.interactable = isSpot && !autoSpotAngle.val;
        }

        private void FixedUpdate()
        {
            if(target == null || !enableLookAt.val || lightType.val != "Spot")
            {
                return;
            }

            try
            {
                control.transform.LookAt(target.followWhenOff.position);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        public JSONClass Serialize()
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = light.containingAtom.uid;
            json["enableLookAt"].AsBool = activeEnableLookAtVal;
            json["autoIntensity"].AsBool = autoIntensity.val;
            json["autoRange"].AsBool = autoRange.val;
            json["autoSpotAngle"].AsBool = activeAutoSpotAngleVal;
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
        }

        private void OnDisable()
        {
            SetOnColor(UI.defaultOnColor);
        }
    }
}

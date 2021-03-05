﻿using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lumination
{
    internal class LightControl : MonoBehaviour
    {
        public static readonly List<string> types = new List<string> { "Spot", "Point" };

        private Log log = new Log(nameof(LightControl));
        public JSONStorable light;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;

        private float baseIntensityFactor;
        private float rangeDiff;
        private float spotBaseWidth;

        public JSONStorableBool on;
        public JSONStorableColor lightColor;
        public JSONStorableBool enableLookAt;
        private bool activeEnableLookAtVal;
        public JSONStorableBool autoRange;
        public JSONStorableBool autoIntensity;
        private bool activeAutoIntensityVal;
        public JSONStorableBool autoSpotAngle;
        private bool activeAutoSpotAngleVal;
        public JSONStorableFloat distanceFromTarget;

        public JSONStorableStringChooser lightType;
        public JSONStorableFloat range;
        public JSONStorableFloat intensity;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat shadowStrength;

        public string targetUid;
        public string prevLightType;

        public void Init(JSONStorable light, FreeControllerV3 control, string lightType)
        {
            this.light = light;
            this.control = control;
            SetOnStyle();
            activeEnableLookAtVal = lightType == "Spot";
            activeAutoIntensityVal = false;
            activeAutoSpotAngleVal = false;
            InitStorables(lightTypeVal: lightType);
        }

        public void InitFromJson(JSONStorable light, FreeControllerV3 control, JSONClass json)
        {
            try
            {
                this.light = light;
                this.control = control;
                SetOnStyle();
                bool isSpotLight = light.GetStringChooserParamValue("type") == "Spot";
                activeEnableLookAtVal = isSpotLight && json["enableLookAt"].AsBool;
                activeAutoIntensityVal = json["autoIntensity"].AsBool;
                activeAutoSpotAngleVal = isSpotLight && json["autoSpotAngle"].AsBool;
                InitStorables(
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
                        log.Message($"Unable to point '{light.containingAtom.uid}' at atom " +
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
                log.Error($"Error initalizing from JSON for atom '{light?.containingAtom?.uid}': {e}");
            }
        }

        private void InitStorables(
            bool autoRangeVal = false,
            string lightTypeVal = null
        )
        {
            on = light.GetBoolJSONParam("on");
            lightColor = Tools.CopyColorStorable(light.GetColorJSONParam("color"), true);
            enableLookAt = new JSONStorableBool("Enable aiming at Target", activeEnableLookAtVal);
            autoRange = new JSONStorableBool("Adjust range relative to Target", autoRangeVal);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to Target", activeAutoIntensityVal);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to Target", activeAutoSpotAngleVal);
            distanceFromTarget = new JSONStorableFloat("Distance from Target", 0f, 0f, 25f);
            lightType = CreateLightTypeStorable(lightTypeVal);
            range = Tools.CopyFloatStorable(light.GetFloatJSONParam("range"), true);
            intensity = Tools.CopyFloatStorable(light.GetFloatJSONParam("intensity"), true);
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
                (val) =>
                {
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
                    targetUid = string.Copy(target.containingAtom.uid);
                    //distanceFromTarget.slider.interactable = true;
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

        public void SetOnStyle(bool selected = false, bool reset = false)
        {
            if(control == null)
            {
                return;
            }

            control.onColor = reset ? UI.defaultOnColor : (selected ? UI.turquoise : UI.lightGray);
            control.highlighted = false; // trigger color change
            control.deselectedMeshScale = reset ? 0.02f : (selected ? 0.03f : 0.02f);
        }

        public void SetInteractableElements()
        {
            distanceFromTarget.slider.interactable = target != null;
            UpdateInteractableByAutoRange(autoRange.val);
            UpdateInteractableByAutoIntensity(autoIntensity.val);
            UpdateInteractableByAutoSpotAngle(autoSpotAngle.val);
            UpdateInteractableByType(lightType.val);
        }

        public void AddInteractableListeners()
        {
            autoRange.toggle.onValueChanged.AddListener(UpdateInteractableByAutoRange);
            autoIntensity.toggle.onValueChanged.AddListener(UpdateInteractableByAutoIntensity);
            autoSpotAngle.toggle.onValueChanged.AddListener(UpdateInteractableByAutoSpotAngle);
            lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange(UpdateInteractableByType);
        }

        private void UpdateInteractableByAutoRange(bool val)
        {
            range.slider.interactable = !val;
            autoIntensity.toggle.interactable = val;
        }

        private void UpdateInteractableByAutoIntensity(bool val)
        {
            intensity.slider.interactable = !val;
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

        public void AddAutoToggleListeners()
        {
            autoRange.toggle.onValueChanged.AddListener((val) =>
            {
                if(control != null && target != null && val)
                {
                    rangeDiff = range.val - CalculateDistance();
                }

                //uncheck autoIntensity toggle if not first adjusting to range
                if(val)
                {
                    autoIntensity.val = activeAutoIntensityVal;
                }
                else
                {
                    activeAutoIntensityVal = autoIntensity.val;
                    autoIntensity.val = false;
                }
            });

            autoIntensity.toggle.onValueChanged.AddListener((val) =>
            {
                if(control != null && target != null && val)
                {
                    baseIntensityFactor = intensity.val / range.val;
                }
            });

            autoSpotAngle.toggle.onValueChanged.AddListener((val) =>
            {
                if(control != null && target != null && val)
                {
                    //calculate width of the side of an isosceles triangle opposite to the spot angle
                    //assuming spot angle is the vertex angle of the triangle
                    //and distance from target is the height of the triangle
                    spotBaseWidth = 2 * CalculateDistance() * Mathf.Tan(Mathf.Deg2Rad * spotAngle.val / 2);
                }
            });
        }

        private float CalculateDistance()
        {
            return Vector3.Distance(control.followWhenOff.position, target.followWhenOff.position);
        }

        private void Update()
        {
            if(control == null || target == null)
            {
                return;
            }

            try
            {
                distanceFromTarget.val = CalculateDistance();

                if(autoRange.val)
                {
                    //keep the "backdrop length" (rangeDiff) of the light relative to target constant
                    range.val = distanceFromTarget.val + rangeDiff;

                    if(autoIntensity.val)
                    {
                        //keep intensity at target more or less constant if auto-adjusting range
                        intensity.val = range.val * baseIntensityFactor;
                    }
                }

                if(autoSpotAngle.val)
                {
                    //calculate angle to match the constant spotVertexAngle based on distance (triangle height)
                    spotAngle.val = 180 - (2 * Mathf.Rad2Deg * Mathf.Atan((2 * distanceFromTarget.val)/spotBaseWidth));
                }
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void FixedUpdate()
        {
            if(control == null || target == null)
            {
                return;
            }

            try
            {
                if(enableLookAt.val && lightType.val == "Spot")
                {
                    control.transform.LookAt(target.followWhenOff.position);
                }
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
            json["autoRange"].AsBool = autoRange.val;
            json["autoIntensity"].AsBool = autoIntensity.val;
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
            SetOnStyle();
        }

        private void OnDisable()
        {
            SetOnStyle(reset: true);
        }
    }
}

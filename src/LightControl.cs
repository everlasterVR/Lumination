using SimpleJSON;
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
        public JSONStorableBool autoRange;
        public JSONStorableBool autoIntensity;
        public JSONStorableBool autoSpotAngle;
        public JSONStorableFloat distanceFromTarget;
        private float prevDistanceFromTargetVal;

        private PointerStatus pointerStatus;

        public JSONStorableStringChooser lightType;
        public JSONStorableFloat range;
        public JSONStorableFloat intensity;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat pointBias;
        public JSONStorableFloat shadowStrength;

        public bool hasTarget = false;
        public string prevLightType;

        public void Init(JSONStorable light, FreeControllerV3 control, string lightType)
        {
            this.light = light;
            this.control = control;
            SetTransformIconStyle();
            InitStorables(lightTypeVal: lightType);
        }

        public void InitFromJson(JSONStorable light, FreeControllerV3 control, JSONClass json)
        {
            try
            {
                this.light = light;
                this.control = control;
                SetTransformIconStyle();

                bool isSpotLight = light.GetStringChooserParamValue("type") == "Spot";
                InitStorables(
                    isSpotLight && json["enableLookAt"].AsBool,
                    json["autoRange"].AsBool,
                    json["autoIntensity"].AsBool,
                    isSpotLight && json["autoSpotAngle"].AsBool
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
                        return;
                    }

                    control.physicsEnabled = true;
                    this.target = target;
                    hasTarget = true;
                }
            }
            catch(Exception e)
            {
                log.Error($"Error initalizing from JSON for atom '{light?.containingAtom?.uid}': {e}");
            }
        }

        private void InitStorables(
            bool enableLookAtVal = false,
            bool autoRangeVal = false,
            bool autoIntensityVal = false,
            bool autoSpotAngleVal = false,
            string lightTypeVal = null
        )
        {
            on = light.GetBoolJSONParam("on");
            lightColor = Tools.CopyColorStorable(light.GetColorJSONParam("color"), true);
            enableLookAt = new JSONStorableBool("Enable aiming at Target", enableLookAtVal);
            autoRange = new JSONStorableBool("Adjust range relative to Target", autoRangeVal);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to Target", autoIntensityVal);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to Target", autoSpotAngleVal);
            distanceFromTarget = new JSONStorableFloat("Distance from Target", 0f, 0f, 5f, false);
            lightType = CreateLightTypeStorable(lightTypeVal);
            range = Tools.CopyFloatStorable(light.GetFloatJSONParam("range"), true);
            intensity = Tools.CopyFloatStorable(light.GetFloatJSONParam("intensity"), true);
            spotAngle = Tools.CopyFloatStorable(light.GetFloatJSONParam("spotAngle"), true);
            pointBias = Tools.CopyFloatStorable(light.GetFloatJSONParam("pointBias"), true);
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
                (val) => source.val = val //update type if type changed in plugin UI
            );
            copy.val = lightTypeVal ?? source.val;

            source.setJSONCallbackFunction = (jc) =>
            {
                if(!types.Contains(jc.val))
                {
                    log.Message($"'{jc.val}' type is not supported. Defaulting to 'Point'.");
                    jc.val = "Point"; //default to Point light
                }
                copy.val = jc.val;
            };

            return copy;
        }

        public void SetSliderClickMonitor(PointerStatus pointerStatus)
        {
            this.pointerStatus = pointerStatus;
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
                    hasTarget = true;
                    enableLookAt.val = lightType.val == "Spot";
                    UpdateInteractablesAndStyles(false);
                })
            );

            while(waiting)
            {
                yield return null;
            }
            string newTargetString = GetTargetString();
            callback(newTargetString == currentTargetString ? null : newTargetString);
        }

        public void SetTransformIconStyle(bool selected = false, bool reset = false)
        {
            if(control == null)
            {
                return;
            }

            control.onColor = reset ? UI.defaultOnColor : (selected ? UI.violet : UI.offGrayViolet);
            control.highlighted = false; // trigger color change
            control.deselectedMeshScale = reset ? 0.02f : (selected ? 0.03f : 0.02f);
        }

        #region Listeners

        public void AddListeners()
        {
            enableLookAt.toggle.onValueChanged.AddListener(OnEnableLookAtToggled);
            autoRange.toggle.onValueChanged.AddListener(OnAutoRangeToggled);
            autoIntensity.toggle.onValueChanged.AddListener(OnAutoIntensityToggled);
            autoSpotAngle.toggle.onValueChanged.AddListener(OnAutoSpotAngleToggled);
            lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange(OnLightTypeChanged);
        }

        public void UpdateInteractablesAndStyles(bool triggerOnLightTypeChange = true)
        {
            OnEnableLookAtToggled(enableLookAt.val);
            OnAutoRangeToggled(autoRange.val);
            OnAutoIntensityToggled(autoIntensity.val);
            OnAutoSpotAngleToggled(autoSpotAngle.val);
            distanceFromTarget.slider.interactable = hasTarget;

            if(triggerOnLightTypeChange)
            {
                OnLightTypeChanged(lightType.val);
            }
        }

        private void OnEnableLookAtToggled(bool val)
        {
            bool isSpot = lightType.val == "Spot";
            enableLookAt.toggle.interactable = hasTarget && (val || isSpot);
            gameObject.GetComponent<Lights>().UpdateEnableLookAtUIToggle(hasTarget, isSpot);
        }

        private void OnAutoRangeToggled(bool val)
        {
            range.slider.interactable = !val;
            intensity.slider.interactable = !val || !autoIntensity.val;
            autoRange.toggle.interactable = hasTarget;
            autoIntensity.toggle.interactable = hasTarget && (autoIntensity.val || val);
            gameObject.GetComponent<Lights>().UpdateAutoRangeUIToggle(hasTarget);
            gameObject.GetComponent<Lights>().UpdateAutoIntensityUIToggle(hasTarget, val);

            if(control != null && hasTarget && val)
            {
                rangeDiff = range.val - CalculateDistance();
            }
        }

        private void OnAutoIntensityToggled(bool val)
        {
            intensity.slider.interactable = !val || !autoRange.val;
            autoIntensity.toggle.interactable = hasTarget && (val || autoRange.val);
            gameObject.GetComponent<Lights>().UpdateAutoIntensityUIToggle(hasTarget, autoRange.val);

            if(control != null && hasTarget && val)
            {
                baseIntensityFactor = intensity.val / range.val;
            }
        }

        private void OnAutoSpotAngleToggled(bool val)
        {
            bool isSpot = lightType.val == "Spot";
            autoSpotAngle.toggle.interactable = hasTarget && (val || isSpot);
            gameObject.GetComponent<Lights>().UpdateAutoSpotAngleUIToggle(hasTarget, isSpot);

            //spotAngle slider is null when autoSpotAngle.val is restored to activeAutoSpotAngleVal
            //after the light type Point->Spot change, but the slider doesn't exist for Point lights
            if(isSpot && spotAngle.slider != null)
            {
                spotAngle.slider.interactable = !val;
            }

            if(control != null && hasTarget && val)
            {
                //calculate width of the side of an isosceles triangle opposite to the spot angle
                //assuming spot angle is the vertex angle of the triangle
                //and distance from target is the height of the triangle
                spotBaseWidth = 2 * CalculateDistance() * Mathf.Tan(Mathf.Deg2Rad* spotAngle.val / 2);
            }
        }

        private void OnLightTypeChanged(string val)
        {
            bool isSpot = val == "Spot";
            Lights lights = gameObject.GetComponent<Lights>();
            enableLookAt.toggle.interactable = hasTarget && (enableLookAt.val || isSpot);
            lights.UpdateEnableLookAtUIToggle(hasTarget, isSpot);
            lights.UpdateAutoSpotAngleUIToggle(hasTarget, isSpot);
        }

        #endregion Listeners

        private float CalculateDistance()
        {
            return Vector3.Distance(control.followWhenOff.position, target.followWhenOff.position);
        }

        private void FixedUpdate()
        {
            try
            {
                if(control == null || !hasTarget)
                {
                    return;
                }

                if(enableLookAt.val && lightType.val == "Spot")
                {
                    control.transform.LookAt(target.followWhenOff.position);
                }

                float distance = CalculateDistance();

                if(autoSpotAngle.val)
                {
                    //calculate angle to match the constant spotVertexAngle based on distance (triangle height)
                    spotAngle.val = 180 - (2 * Mathf.Rad2Deg * Mathf.Atan((2 * distance)/spotBaseWidth));
                }

                if(pointerStatus != null && distanceFromTarget != null)
                {
                    if(pointerStatus.isDown && pointerStatus.changed)
                    {
                        distanceFromTarget.slider.onValueChanged.AddListener(DistanceFromTargetListener);
                    }

                    if(!pointerStatus.isDown)
                    {
                        if(pointerStatus.changed)
                        {
                            distanceFromTarget.slider.onValueChanged.RemoveListener(DistanceFromTargetListener);
                        }
                        distanceFromTarget.val = distance;
                        prevDistanceFromTargetVal = distanceFromTarget.val;
                    }
                }

                if(autoRange.val)
                {
                    //keep the "backdrop length" (rangeDiff) of the light relative to target constant
                    range.val = distance + rangeDiff;

                    if(autoIntensity.val)
                    {
                        //keep intensity at target more or less constant if auto-adjusting range
                        intensity.val = range.val * baseIntensityFactor;
                    }
                }
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        public void DistanceFromTargetListener(float val)
        {
            if(val < 0.2f)
            {
                val = 0.2f;
            }
            else if(val > 25)
            {
                val = 25;
            }

            Vector3 direction = (target.followWhenOff.position - control.followWhenOff.position).normalized;
            control.transform.Translate(direction * (prevDistanceFromTargetVal - val), Space.World);
            prevDistanceFromTargetVal = val;
        }

        public string GetTargetUID()
        {
            return target?.containingAtom.uid;
        }

        public string GetTargetString()
        {
            if(!hasTarget)
            {
                return null;
            }

            return $"\n{target.containingAtom.uid}:{target.name}";
        }

        public string GetButtonLabelTargetString()
        {
            if(!hasTarget)
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

        public JSONClass Serialize()
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = light.containingAtom.uidWithoutSubScenePath;
            json["enableLookAt"].AsBool = enableLookAt.val;
            json["autoRange"].AsBool = autoRange.val;
            json["autoIntensity"].AsBool = autoIntensity.val;
            json["autoSpotAngle"].AsBool = autoSpotAngle.val;
            if(hasTarget)
            {
                json["aimingAtAtomUid"] = target.containingAtom.uid;
                json["aimingAtControl"] = target.name;
            }
            return json;
        }

        private void OnEnable()
        {
            SetTransformIconStyle();
        }

        private void OnDisable()
        {
            SetTransformIconStyle(reset: true);
        }
    }
}

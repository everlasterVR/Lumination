using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lumination
{
    internal class LightControl : MonoBehaviour
    {
        public static readonly List<string> types = new List<string> { "Spot", "Point" };

        private Log log = new Log(nameof(LightControl));
        private Lights lights;
        public JSONStorable light;
        public FreeControllerV3 control;
        public FreeControllerV3 target;
        public UIDynamicButton uiButton;

        private float distanceFromTarget;
        private float baseIntensityFactor;
        private float rangeDiff;
        private float spotBaseWidth;

        public JSONStorableBool on;
        public JSONStorableColor lightColor;
        public JSONStorableBool enableLookAt;
        public JSONStorableBool autoRange;
        public JSONStorableBool autoIntensity;
        public JSONStorableBool autoSpotAngle;
        public JSONStorableFloat distance;
        private float prevDistanceVal;

        public JSONStorableStringChooser lightType;
        public JSONStorableFloat range;
        public JSONStorableFloat intensity;
        public JSONStorableFloat spotAngle;
        public JSONStorableFloat pointBias;
        public JSONStorableFloat shadowStrength;

        private DistanceSliderClickHandler distanceSCH;
        private RangeSliderClickHandler rangeSCH;
        private IntensitySliderClickHandler intensitySCH;
        private SpotAngleSliderClickHandler spotAngleSCH;

        public bool hasTarget = false;
        public string prevLightType;

        public void Init(JSONStorable light, FreeControllerV3 control, string lightTypeVal)
        {
            lights = gameObject.GetComponent<Lights>();
            this.light = light;
            this.control = control;
            this.control.SetBoolParamValue("physicsEnabled", true);
            this.control.SetBoolParamValue("useGravity", false);
            this.control.SetFloatParamValue("mass", 0.01f);
            SetTransformIconStyle();
            InitStorables(lightTypeVal);
        }

        public void InitFromJson(JSONStorable light, FreeControllerV3 control, JSONClass json)
        {
            try
            {
                lights = gameObject.GetComponent<Lights>();
                this.light = light;
                this.control = control;
                this.control.SetBoolParamValue("physicsEnabled", true);
                this.control.SetBoolParamValue("useGravity", false);
                SetTransformIconStyle();

                InitStorables();

                FreeControllerV3 target = null;
                if(json["aimingAtAtomUid"] != null && json["aimingAtControl"] != null)
                {
                    string aimingAtAtomUid = json["aimingAtAtomUid"].Value;
                    string aimingAtControl = json["aimingAtControl"].Value;
                    Atom aimingAtAtom = SuperController.singleton.GetAtomByUid(aimingAtAtomUid);
                    target = aimingAtAtom.freeControllers.Where(it => it.name == aimingAtControl).FirstOrDefault();

                    //should not occur unless json modified manually
                    if(target == null)
                    {
                        log.Error($"Unable to point '{light.containingAtom.uid}' at atom " +
                            $"'{aimingAtAtomUid}' target control '{aimingAtControl}': " +
                            $"target mentioned in saved JSON but not found in scene.");
                        return;
                    }

                    this.target = target;
                    hasTarget = true;

                    bool isSpotLight = light.GetStringChooserParamValue("type") == "Spot";
                    enableLookAt.val = isSpotLight && json["enableLookAt"].AsBool;
                    autoRange.val = json["autoRange"].AsBool;
                    autoIntensity.val = json["autoRange"].AsBool && json["autoIntensity"].AsBool;
                    autoSpotAngle.val = isSpotLight && json["autoSpotAngle"].AsBool;

                    //setup base values for auto-adjusting to work properly when the light's UI hasn't yet been opened
                    if(autoRange.val)
                    {
                        UpdateRangeDiff();
                    }
                    if(autoIntensity.val)
                    {
                        UpdateBaseIntensityFactor();
                    }
                    if(autoSpotAngle.val)
                    {
                        UpdateSpotBaseWidth();
                    }
                }
            }
            catch(Exception e)
            {
                log.Error($"Error initalizing from JSON for atom '{light?.containingAtom?.uid}': {e}");
            }
        }

        private void InitStorables(string lightTypeVal = null)
        {
            on = light.GetBoolJSONParam("on");
            lightColor = Tools.CopyColorStorable(light.GetColorJSONParam("color"), true);
            enableLookAt = new JSONStorableBool("Enable aiming at Target", false);
            autoRange = new JSONStorableBool("Adjust range relative to Target", false);
            autoIntensity = new JSONStorableBool("Adjust intensity relative to Target", false);
            autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to Target", false);
            distance = new JSONStorableFloat("Distance from Target", 0f, 0f, 5f, false);
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

                //same as popup change handler in PostCreateLightControlUI
                if(prevLightType != copy.val && lights.selectedUid == light.containingAtom.uid)
                {
                    lights.RefreshUI(light.containingAtom.uid);
                }
                prevLightType = copy.val;
                UpdateLightAtomUID();
            };

            return copy;
        }

        public void UpdateLightAtomUID()
        {
            Atom atom = light.containingAtom;
            string aimAt = GetButtonLabelTargetString();
            string basename = lightType.val + (string.IsNullOrEmpty(aimAt) ? "" : $"-{aimAt}");

            //prevent rename of atom if only the number sequence differs
            if(basename == Regex.Split(Tools.WithoutSubScenePath(atom.uid), "#\\d")[0])
            {
                return;
            }
            atom.SetUID(Tools.NewUID(basename));
        }

        public void AddSliderClickMonitors()
        {
            distanceSCH = distance.slider.gameObject.AddComponent<DistanceSliderClickHandler>();
            rangeSCH = range.slider.gameObject.AddComponent<RangeSliderClickHandler>();
            intensitySCH = intensity.slider.gameObject.AddComponent<IntensitySliderClickHandler>();
            distanceSCH.Init(this);
            rangeSCH.Init(this);
            intensitySCH.Init(this);

            if(lightType.val == "Spot")
            {
                spotAngleSCH = spotAngle.slider.gameObject.AddComponent<SpotAngleSliderClickHandler>();
                spotAngleSCH.Init(this);
            }
        }

        public void RemoveSliderClickMonitors()
        {
            Destroy(distanceSCH);
            Destroy(rangeSCH);
            Destroy(intensitySCH);
            Destroy(spotAngleSCH);
        }

        public IEnumerator OnSelectTarget(Action<string> callback)
        {
            control.physicsEnabled = true;
            bool waiting = true;
            string currentTargetString = GetTargetString();

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(targetCtrl =>
                {
                    if(targetCtrl == target)
                    {
                        return;
                    }

                    if(targetCtrl.containingAtom == control.containingAtom)
                    {
                        log.Message($"Come on, don't point the light at itself. The universe might implode.");
                        return;
                    }

                    if(targetCtrl.containingAtom.type == Const.INVLIGHT)
                    {
                        log.Error($"Targeting another {Const.INVLIGHT} atom is not supported.");
                        return;
                    }

                    waiting = false;
                    target = targetCtrl;
                    if(!hasTarget)
                    {
                        enableLookAt.val = lightType.val == "Spot";
                    }
                    hasTarget = true;
                    TriggerListeners();
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

        public void TriggerListeners(bool triggerOnLightTypeChange = true)
        {
            OnEnableLookAtToggled(enableLookAt.val);
            OnAutoRangeToggled(autoRange.val);
            OnAutoIntensityToggled(autoIntensity.val);
            OnAutoSpotAngleToggled(autoSpotAngle.val);
            distance.slider.interactable = hasTarget;

            if(triggerOnLightTypeChange)
            {
                OnLightTypeChanged(lightType.val);
            }
        }

        private void OnEnableLookAtToggled(bool val)
        {
            bool isSpot = lightType.val == "Spot";
            enableLookAt.toggle.interactable = hasTarget && (val || isSpot);
            lights.UpdateEnableLookAtUIToggle(hasTarget, isSpot);
        }

        private void OnAutoRangeToggled(bool val)
        {
            autoRange.toggle.interactable = hasTarget;
            autoIntensity.toggle.interactable = hasTarget && (autoIntensity.val || val);
            lights.UpdateAutoRangeUIToggle(hasTarget);
            lights.UpdateAutoIntensityUIToggle(hasTarget, val);

            if(control != null && hasTarget && val)
            {
                UpdateRangeDiff();
            }
        }

        private void OnAutoIntensityToggled(bool val)
        {
            autoIntensity.toggle.interactable = hasTarget && (val || autoRange.val);
            lights.UpdateAutoIntensityUIToggle(hasTarget, autoRange.val);

            if(control != null && hasTarget && val)
            {
                UpdateBaseIntensityFactor();
            }
        }

        private void OnAutoSpotAngleToggled(bool val)
        {
            bool isSpot = lightType.val == "Spot";
            autoSpotAngle.toggle.interactable = hasTarget && (val || isSpot);
            lights.UpdateAutoSpotAngleUIToggle(hasTarget, isSpot);

            if(control != null && hasTarget && val)
            {
                UpdateSpotBaseWidth();
            }
        }

        private void OnLightTypeChanged(string val)
        {
            bool isSpot = val == "Spot";
            enableLookAt.toggle.interactable = hasTarget && (enableLookAt.val || isSpot);
            lights.UpdateEnableLookAtUIToggle(hasTarget, isSpot);
            lights.UpdateAutoSpotAngleUIToggle(hasTarget, isSpot);
        }

        public void OnDistanceChanged(float val)
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
            control.transform.Translate(direction * (prevDistanceVal - val), Space.World);
            prevDistanceVal = val;
        }

        #endregion Listeners

        private float CalculateDistance()
        {
            return Vector3.Distance(control.transform.position, target.transform.position);
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
                    control.transform.LookAt(target.transform.position);
                }

                distanceFromTarget = CalculateDistance();

                if(!(distanceSCH?.isDown ?? false))
                {
                    UpdateDistanceVal();
                }

                if(autoRange.val && !(rangeSCH?.isDown ?? false))
                {
                    UpdateRangeVal();
                    if(autoIntensity.val && !(intensitySCH?.isDown ?? false))
                    {
                        UpdateIntensityVal();
                    }
                }

                if(autoSpotAngle.val && !(spotAngleSCH?.isDown ?? false))
                {
                    UpdateSpotAngleVal();
                }
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        public void UpdateDistanceVal()
        {
            distance.val = distanceFromTarget;
            prevDistanceVal = distance.val;
        }

        public void UpdateRangeDiff()
        {
            rangeDiff = range.val - CalculateDistance();
        }

        //keep the "backdrop length" (rangeDiff) of the light relative to target constant
        public void UpdateRangeVal()
        {
            range.val = distanceFromTarget + rangeDiff;
        }

        public void UpdateBaseIntensityFactor()
        {
            baseIntensityFactor = intensity.val / range.val;
        }

        //keep intensity at target more or less constant if auto-adjusting range
        public void UpdateIntensityVal()
        {
            intensity.val = range.val * baseIntensityFactor;
        }

        //calculate width of the side of an isosceles triangle opposite to the spot angle
        //assuming spot angle is the vertex angle of the triangle
        //and distance from target is the height of the triangle
        public void UpdateSpotBaseWidth()
        {
            spotBaseWidth = 2 * CalculateDistance() * Mathf.Tan(Mathf.Deg2Rad * spotAngle.val / 2);
        }

        //calculate angle to match the constant spotVertexAngle based on distance (triangle height)
        public void UpdateSpotAngleVal()
        {
            spotAngle.val = 180 - (2 * Mathf.Rad2Deg* Mathf.Atan((2 * distanceFromTarget)/spotBaseWidth));
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

            return $"{target.containingAtom.uid}:{target.name}";
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

        public JSONClass Serialize(bool forceSaveTarget)
        {
            JSONClass json = new JSONClass();
            json["atomUid"] = light.containingAtom.uidWithoutSubScenePath;
            if(!hasTarget)
            {
                return json;
            }

            Func<bool> TargetInSubScene = () => lights.containingAtom.subSceneComponent.atomsInSubScene.Contains(target.containingAtom);
            if(forceSaveTarget || TargetInSubScene())
            {
                json["aimingAtAtomUid"] = target.containingAtom.uid;
                json["aimingAtControl"] = target.name;
                if(enableLookAt.val)
                {
                    json["enableLookAt"].AsBool = enableLookAt.val;
                }
                if(autoRange.val)
                {
                    json["autoRange"].AsBool = autoRange.val;
                }
                if(autoIntensity.val)
                {
                    json["autoIntensity"].AsBool = autoIntensity.val;
                }
                if(autoSpotAngle.val)
                {
                    json["autoSpotAngle"].AsBool = autoSpotAngle.val;
                }
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

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Illumination
{
    internal class Script : MVRScript
    {
        private const string version = "<Version>";
        private const string atomUidPrefix = "Illum_";
        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();

        private JSONStorableBool disableOtherLights;
        private List<Atom> disabledLights = new List<Atom>();

        private UIDynamicButton selectTargetButton;
        private string selected = "";

        private UIDynamicButton removeLightButton;
        private UIDynamic leftUISpacer;
        private UIDynamicColorPicker lightColorPicker;

        private bool restoringFromJson = false;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                //left side UI

                TitleUITextField();
                DisableOtherLightsUIToggle();

                UIDynamicButton addSpotLightButton = CreateButton("Add new spotlight");
                addSpotLightButton.button.onClick.AddListener(() => AddNewInvisibleLight());
                UI.DecreaseAvailableHeight(addSpotLightButton.height);

                UIDynamicButton addLightFromSceneButton = CreateButton("Add light from scene");
                addLightFromSceneButton.button.onClick.AddListener(() => AddSelectedInvisibleLight());
                UI.DecreaseAvailableHeight(addLightFromSceneButton.height);

                removeLightButton = CreateButton("Remove selected atom");
                removeLightButton.button.onClick.AddListener(() => RemoveSelectedInvisibleLight());
                UI.DecreaseAvailableHeight(removeLightButton.height);

                UISpacer(15f);

                leftUISpacer = CreateSpacer();
                leftUISpacer.height = UI.GetAvailableHeight();

                //right side UI

                selectTargetButton = CreateButton("Select target to aim at", true);
                selectTargetButton.height = 100f;

                JSONStorableBool enableAimAtTarget = new JSONStorableBool("Enable aiming at target", false);
                UIDynamicToggle enableAimAtTargetToggle = CreateToggle(enableAimAtTarget, true);
                JSONStorableBool autoIntensity = new JSONStorableBool("Adjust intensity relative to target", false);
                UIDynamicToggle autoIntensityToggle = CreateToggle(autoIntensity, true);
                JSONStorableBool autoRange = new JSONStorableBool("Adjust range relative to target", false);
                UIDynamicToggle autoRangeToggle = CreateToggle(autoRange, true);
                JSONStorableBool autoSpotAngle = new JSONStorableBool("Adjust spot angle relative to target", false);
                UIDynamicToggle autoSpotAngleToggle = CreateToggle(autoSpotAngle, true);

                UISpacer(15f, true);

                JSONStorableFloat intensity = new JSONStorableFloat("Intensity", 1, 0.1f, 5);
                UIDynamicSlider intensitySlider = CreateSlider(intensity, true);
                intensitySlider.valueFormat = "F2";

                JSONStorableFloat range = new JSONStorableFloat("Range", 5, 1, 25);
                UIDynamicSlider rangeSlider = CreateSlider(range, true);
                rangeSlider.valueFormat = "F1";

                UIDynamicPopup lightType = CreatePopup(new JSONStorableStringChooser(
                    "lightType",
                    new List<string> { "Spot", "Point" },
                    "Spot",
                    "Light Type"
                ), true);

                JSONStorableFloat spotAngle = new JSONStorableFloat("Spot angle", 80, 10, 150);
                UIDynamicSlider spotAngleSlider = CreateSlider(spotAngle, true);
                spotAngleSlider.valueFormat = "F0";

                JSONStorableFloat shadowStrength = new JSONStorableFloat("Shadow strength", 0.2f, 0, 1);
                UIDynamicSlider shadowStrengthSlider = CreateSlider(shadowStrength, true);
                shadowStrengthSlider.valueFormat = "F2";

                //rest

                RefreshUI("");
                AddSuperControllerOnAtomActions();

                StartCoroutine(AddExistingILAtoms());
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private IEnumerator AddExistingILAtoms()
        {
            yield return new WaitForEndOfFrame();

            while(restoringFromJson)
            {
                yield return null;
            }

            GetSceneAtoms().ForEach(atom =>
            {
                if(atom.type != "InvisibleLight" || !atom.uid.StartsWith(atomUidPrefix) || lightControls.ContainsKey(atom.uid))
                {
                    return;
                }

                Light light = atom.GetComponentInChildren<Light>();
                if(light.type != LightType.Point && light.type != LightType.Spot)
                {
                    return;
                }

                AddExistingILAtomToPlugin(atom, $"{light.type}");
            });
        }

        private void TitleUITextField()
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable);
            field.UItext.fontSize = 36;
            field.height = 100;
            UI.DecreaseAvailableHeight(field.height);
            storable.val = $"<b>{nameof(Illumination)}</b>\n<size=28>v{version}</size>";
        }

        private void DisableOtherLightsUIToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot lights", false);
            UIDynamicToggle disableOtherLightsToggle = CreateToggle(disableOtherLights);
            UI.DecreaseAvailableHeight(disableOtherLightsToggle.height);
            disableOtherLights.toggle.onValueChanged.AddListener(val =>
            {
                if(val)
                {
                    DisableOtherPointAndSpotLights();
                }
                else
                {
                    EnableDisabledLights();
                }
            });
        }

        private void AddNewInvisibleLight()
        {
            if(lightControls.Count >= 6)
            {
                Log.Message("You have the maximum number of lights.");
                return;
            }

            StartCoroutine(Tools.CreateAtomCo("InvisibleLight", $"{atomUidPrefix}InvisibleLight", (atom) =>
            {
                AddExistingILAtomToPlugin(atom, "Spot");
            }));
        }

        private void AddSelectedInvisibleLight()
        {
            try
            {
                SuperController.singleton.SelectModeControllers(
                    new SuperController.SelectControllerCallback(targetCtrl =>
                    {
                        Atom atom = targetCtrl.containingAtom;
                        if(atom.type != "InvisibleLight")
                        {
                            Log.Message("Selected atom is not an InvisibleLight atom!");
                            return;
                        }

                        if(lightControls.ContainsKey(atom.uid))
                        {
                            Log.Message("Selected InvisibleLight is already added!");
                            return;
                        }

                        if(!atom.on)
                        {
                            atom.ToggleOn();
                        }

                        JSONStorable light = atom.GetStorableByID("Light");
                        string lightType = $"{light.GetStringChooserParamValue("type")}";

                        if(lightType != "Spot" && lightType != "Point")
                        {
                            Log.Message("Only Spot and Point lights are supported.");
                            return;
                        }

                        light.SetBoolParamValue("on", true);
                        AddExistingILAtomToPlugin(atom, lightType);
                    })
                );
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void AddExistingILAtomToPlugin(Atom atom, string lightType)
        {
            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.Init(atom, lightType);
            string uid = atom.uid;

            if(lightColorPicker != null)
            {
                UI.IncreaseAvailableHeight(lightColorPicker.height);
                RemoveColorPicker(lightColorPicker);
            }
            lc.uiButton = UILightButton(uid);
            lightControls.Add(uid, lc);
            RefreshUI(uid);

            if(lightControls.Count > 0)
            {
                CreateLightColorPicker();
                UI.DecreaseAvailableHeight(lightColorPicker.height);
                leftUISpacer.height -= lightColorPicker.height + 15f;
            }

            removeLightButton.button.interactable = true;
        }

        private void RemoveSelectedInvisibleLight()
        {
            //if(lightControls != null && lightControls.ContainsKey(selected))
            if(lightControls.ContainsKey(selected))
            {
                SuperController.singleton.RemoveAtom(lightControls[selected].lightAtom);
            }
        }

        private void CreateLightColorPicker()
        {
            JSONStorableColor lightColor = new JSONStorableColor("Light Color", HSVColorPicker.RGBToHSV(255, 228, 199));
            lightColorPicker = CreateColorPicker(lightColor);
        }

        private void UISpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
            UI.DecreaseAvailableHeight(height, rightSide);
        }

        private UIDynamicButton UILightButton(string uid)
        {
            RemoveSpacer(leftUISpacer);
            UIDynamicButton uiButton = CreateButton(UI.Color(uid, UI.lightGray));
            UI.DecreaseAvailableHeight(uiButton.height);
            uiButton.buttonColor = UI.black;
            leftUISpacer = CreateSpacer();
            leftUISpacer.height = UI.GetAvailableHeight();
            uiButton.button.onClick.AddListener(() => {
                RefreshUI(uid);
            });
            return uiButton;
        }

        private void RefreshUI(string uid)
        {
            if(lightControls.ContainsKey(selected))
            {
                LightControl selectedLc = lightControls[selected];
                selectedLc.uiButton.label = UI.Color(selected, UI.lightGray);
                selectedLc.SetOnColor(UI.white);
                RemoveToggle(selectedLc.enableLookAt);
            }

            selectTargetButton.button.onClick.RemoveAllListeners();
            selected = uid;

            if(lightControls.ContainsKey(uid))
            {
                LightControl lc = lightControls[uid];
                lc.uiButton.label =  UI.Bold(UI.Color(uid, UI.blue));
                CreateToggle(lc.enableLookAt, true);
                selectTargetButton.button.interactable = true;
                selectTargetButton.label = $"Select target to aim at\n{UI.Italic(UI.Size(lc.GetTargetString(), 26))}";
                lc.enableLookAt.toggle.interactable = lc.target != null;

                selectTargetButton.button.onClick.AddListener(() =>
                {
                    StartCoroutine(lc.OnSelectTarget((targetString) =>
                    {
                        selectTargetButton.label = $"Select target to aim at\n{UI.Italic(UI.Size(targetString, 26))}";
                    }));
                    lc.enableLookAt.toggle.interactable = true;
                });
            }
            else
            {
                removeLightButton.button.interactable = false;
                //TODO empty UI
            }
        }

        public void OnEnable()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => it.enabled = true);
                DisableOtherPointAndSpotLights();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void DisableOtherPointAndSpotLights()
        {
            GetSceneAtoms().ForEach(atom => DisableAtomIfIsOtherLight(atom));
        }

        private bool DisableAtomIfIsOtherLight(Atom atom)
        {
            if(disableOtherLights == null || !disableOtherLights.val)
            {
                return false;
            }

            if(!atom.on || atom.type != "InvisibleLight" || atom.uid.StartsWith(atomUidPrefix) || lightControls.ContainsKey(atom.uid))
            {
                return false;
            }

            Light light = atom.GetComponentInChildren<Light>();
            if(light.type == LightType.Point || light.type == LightType.Spot)
            {
                atom.ToggleOn();
                disabledLights.Add(atom);
                return true;
            }

            return false;
        }

        private void EnableDisabledLights()
        {
            disabledLights?.ForEach(atom =>
            {
                if(!atom.on)
                {
                    atom.ToggleOn();
                }
            });
        }

        private void AddSuperControllerOnAtomActions()
        {
            SuperController.singleton.onAtomAddedHandlers += new SuperController.OnAtomAdded(OnAddAtom);
            SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnRemoveAtom);
            SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnRenameAtom);
        }

        private void OnAddAtom(Atom atom)
        {
            bool wasDisabled = DisableAtomIfIsOtherLight(atom);
            if(wasDisabled)
            {
                Log.Message($"New InvisibleLight '{atom.uid}' was automatically disabled because 'Disable other point and spot lights' is checked in plugin UI.");
            }
        }

        private void OnRemoveAtom(Atom atom)
        {
            //light atom added by plugin was removed elsewhere
            if(lightControls.ContainsKey(atom.uid))
            {
                LightControl lc = lightControls[atom.uid];
                lightControls.Remove(atom.uid);
                UI.IncreaseAvailableHeight(lc.uiButton.height);
                RemoveButton(lc.uiButton);
                leftUISpacer.height = UI.GetAvailableHeight();

                Destroy(lc);

                if(selected == atom.uid)
                {
                    RemoveToggle(lc.enableLookAt);
                }

                RefreshUI(lightControls?.Keys.FirstOrDefault() ?? "");
            }
            if(lightControls.Count == 0 && lightColorPicker != null)
            {
                UI.IncreaseAvailableHeight(lightColorPicker.height);
                RemoveColorPicker(lightColorPicker);
            }

            //other light atom disabled by plugin was removed
            if(disabledLights.Contains(atom))
            {
                disabledLights.Remove(atom);
            }
        }

        private void OnRenameAtom(string fromuid, string touid)
        {
            //light atom added by plugin was renamed elsewhere
            if(lightControls.ContainsKey(fromuid))
            {
                LightControl lc = lightControls[fromuid];
                lightControls.Remove(fromuid);
                lightControls.Add(touid, lc);
                lc.uiButton.label = touid;
                if(selected == fromuid)
                {
                    RefreshUI(touid);
                }
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["lightControls"] = new JSONArray();
            lightControls.Values.ToList().ForEach(lc => json["lightControls"].Add(lc.Serialize()));
            if(selected != "")
            {
                json["selected"] = selected;
            }
            json["disableOtherLights"].AsBool = disableOtherLights.val;
            needsStore = true;
            return json;
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            restoringFromJson = true;

            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(RestoreFromJSONInternal(json));
        }

        private IEnumerator RestoreFromJSONInternal(JSONClass json)
        {
            yield return new WaitForEndOfFrame();

            foreach(JSONClass lightJson in json["lightControls"].AsArray)
            {
                RestoreLightControlFromJSON(lightJson);
            }

            disableOtherLights.val = json["disableOtherLights"].AsBool;
            DisableOtherPointAndSpotLights();
            if(json["selected"] != null)
            {
                string uid = json["selected"].Value;
                if(lightControls.ContainsKey(uid))
                {
                    RefreshUI(uid);
                }
            }

            restoringFromJson = false;
        }

        private void RestoreLightControlFromJSON(JSONClass lightJson)
        {
            string atomUid = lightJson["atomUid"].Value;
            Atom atom = GetAtomById(atomUid);
            if(atom == null)
            {
                Log.Message($"Unable to control light atom '{atomUid}': " +
                    $"mentioned in saved JSON but not found in scene.");
            }
            else
            {
                LightControl lc = gameObject.AddComponent<LightControl>();

                FreeControllerV3 target = null;
                if(lightJson["aimingAtAtomUid"] != null && lightJson["aimingAtControl"] != null)
                {
                    string aimingAtAtomUid = lightJson["aimingAtAtomUid"].Value;
                    string aimingAtControl = lightJson["aimingAtControl"].Value;
                    Atom aimingAtAtom = GetAtomById(aimingAtAtomUid);
                    target = aimingAtAtom?.gameObject
                        .GetComponentsInChildren<FreeControllerV3>()
                        .Where(it => it.name == aimingAtControl)
                        .FirstOrDefault();

                    if(target == null)
                    {
                        Log.Message($"Unable to point light atom '{atomUid}' at atom " +
                            $"'{aimingAtAtomUid}' target control '{aimingAtControl}': " +
                            $"target mentioned in saved JSON but not found in scene.");
                    }
                }

                //duplicated from AddCreatedAtomToPlugin
                lc.InitFromSave(atom, target, lightJson["enableLookAt"].AsBool);
                string uid = atom.uid;

                if(lightColorPicker != null)
                {
                    UI.IncreaseAvailableHeight(lightColorPicker.height);
                    RemoveColorPicker(lightColorPicker);
                }
                lc.uiButton = UILightButton(uid);
                lightControls.Add(uid, lc);
                RefreshUI(uid);

                if(lightControls.Count > 0)
                {
                    CreateLightColorPicker();
                    UI.DecreaseAvailableHeight(lightColorPicker.height);
                    leftUISpacer.height -= lightColorPicker.height + 15f;
                }
            }
        }

        //public void FixedUpdate()
        //{
        //    try
        //    {
        //    }
        //    catch(Exception e)
        //    {
        //        Log.Error($"{e}");
        //    }
        //}

        public void Update()
        {
            try
            {
                if(lightControls != null && lightControls.ContainsKey(selected))
                {
                    lightControls[selected].SetOnColor(UITransform.gameObject.activeInHierarchy ? UI.blue : UI.white);
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        public void OnDisable()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => it.enabled = false);
                EnableDisabledLights();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        public void OnDestroy()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => Destroy(it));
                SuperController.singleton.onAtomAddedHandlers -= new SuperController.OnAtomAdded(OnAddAtom);
                SuperController.singleton.onAtomRemovedHandlers -= new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomUIDRenameHandlers -= new SuperController.OnAtomUIDRename(OnRenameAtom);
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

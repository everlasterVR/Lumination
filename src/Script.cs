using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Illumination
{
    internal class Script : MVRScript
    {
        private const string version = "<Version>";
        private const string atomUidPrefix = "IL_";
        private const string atomType = "InvisibleLight";
        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();
        private SortedDictionary<string, string> atomUidToGuid = new SortedDictionary<string, string>();

        private JSONStorableBool disableOtherLights;
        private List<Atom> disabledLights = new List<Atom>();
        private string selectedUid = "";

        private UIDynamicButton removeLightButton;

        private UIDynamic colorPickerSpacer;
        private UIDynamicColorPicker lightColorPicker;
        private UIDynamicButton selectTargetButton;
        private UIDynamicToggle enableLookAtToggle;
        private UIDynamicToggle autoIntensityToggle;
        private UIDynamicToggle autoRangeToggle;
        private UIDynamicToggle autoSpotAngleToggle;
        private UIDynamic lightTypeSpacer;
        private UIDynamicPopup lightTypePopup;
        private UIDynamicSlider intensitySlider;
        private UIDynamicSlider rangeSlider;
        private UIDynamicSlider spotAngleSlider;
        private UIDynamicSlider shadowStrengthSlider;

        private bool? restoringFromJson;
        private bool? removedFromPluginUI;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                InitUILeft();
                AddSuperControllerOnAtomActions();
                StartCoroutine(AddExistingILAtoms((uid) => RefreshUI(uid)));
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void InitUILeft()
        {
            TitleUITextField();
            AddSpotlightButton();
            AddLightFromSceneButton();
            RemoveLightButton();
            DisableOtherLightsUIToggle();
            UISpacer(10f);
        }

        private void TitleUITextField()
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable);
            field.backgroundColor = UI.defaultPluginBgColor;
            field.textColor = UI.white;
            field.UItext.alignment = TextAnchor.MiddleCenter;
            field.height = 100;
            storable.val = UI.Size("\n", 24) + UI.Bold(UI.Size($"{nameof(Illumination)} {version}", 36));
        }

        private void AddSpotlightButton()
        {
            UIDynamicButton addSpotLightButton = CreateButton("Add new spotlight");
            addSpotLightButton.buttonColor = UI.lightGreen;
            addSpotLightButton.button.onClick.AddListener(() => AddNewInvisibleLight());
        }

        private void AddLightFromSceneButton()
        {
            UIDynamicButton addLightFromSceneButton = CreateButton("Add light from scene");
            addLightFromSceneButton.buttonColor = UI.lightGreen;
            addLightFromSceneButton.button.onClick.AddListener(() => AddSelectedInvisibleLight());
        }

        private void RemoveLightButton()
        {
            removeLightButton = CreateButton("Remove selected atom");
            removeLightButton.buttonColor = UI.pink;
            removeLightButton.button.onClick.AddListener(() => RemoveSelectedInvisibleLight());
        }

        private void DisableOtherLightsUIToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot lights", false);
            UIDynamicToggle disableOtherLightsToggle = CreateToggle(disableOtherLights);
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

        private IEnumerator AddExistingILAtoms(Action<string> callback)
        {
            yield return new WaitForEndOfFrame();

            while(restoringFromJson.GetValueOrDefault(false))
            {
                yield return null;
            }

            GetSceneAtoms()
                .Where(atom => atom.type == atomType && atom.uid.StartsWith(atomUidPrefix) && !lightControls.ContainsKey(atom.uid))
                .OrderBy(atom => atom.uid).ToList()
                .ForEach(atom =>
                {
                    Light light = atom.GetComponentInChildren<Light>();
                    if(light.type != LightType.Point && light.type != LightType.Spot)
                    {
                        return;
                    }

                    AddExistingILAtomToPlugin(atom, $"{light.type}", true);
                });

            if(restoringFromJson == null)
            {
                callback(atomUidToGuid?.Keys.FirstOrDefault() ?? "");
            }
        }

        private void AddNewInvisibleLight()
        {
            if(lightControls.Count >= 6)
            {
                Log.Message("You have the maximum number of pixel lights.");
                return;
            }

            StartCoroutine(Tools.CreateAtomCo(atomType, GenerateUID("Spot"), (atom) =>
            {
                AddExistingILAtomToPlugin(atom, "Spot");
                RefreshUI(atom.uid);
            }));
        }

        private void AddSelectedInvisibleLight()
        {
            if(lightControls.Count >= 6)
            {
                Log.Message("You have the maximum number of lights.");
                return;
            }

            try
            {
                SuperController.singleton.SelectModeControllers(
                    new SuperController.SelectControllerCallback(targetCtrl =>
                    {
                        Atom atom = targetCtrl.containingAtom;
                        if(atom.type != atomType)
                        {
                            Log.Message($"Selected atom is not an {atomType} atom!");
                            return;
                        }

                        if(atomUidToGuid.ContainsKey(atom.uid))
                        {
                            Log.Message($"Selected {atomType} is already added!");
                            return;
                        }

                        JSONStorable light = atom.GetStorableByID("Light");
                        string lightType = light.GetStringChooserParamValue("type");

                        if(!LightControl.types.Contains(lightType))
                        {
                            Log.Message("Only Spot and Point lights are supported.");
                            return;
                        }

                        if(!atom.on)
                        {
                            atom.ToggleOn();
                        }

                        light.SetBoolParamValue("on", true);
                        AddExistingILAtomToPlugin(atom, lightType, true);
                        RefreshUI(atom.uid);
                    })
                );
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private string GenerateUID(string lightType, string aimAt = null)
        {
            string name = atomUidPrefix + lightType;
            if(!string.IsNullOrEmpty(aimAt))
            {
                name += $"-At{aimAt}";
            }
            return Tools.NewUID(name);
        }

        private void AddExistingILAtomToPlugin(Atom atom, string lightType, bool updateUid = false)
        {
            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.Init(atom, lightType);
            if(updateUid)
            {
                UpdateAtomUID(lc); //ensure name is correct when reloading plugin
            }
            lc.uiButton = SelectLightButton(atom.uid, lc.on.val);
            string guid = Guid.NewGuid().ToString();
            atomUidToGuid.Add(atom.uid, guid);
            lightControls.Add(guid, lc);
        }

        private void RemoveSelectedInvisibleLight()
        {
            removedFromPluginUI = true;
            if(atomUidToGuid.ContainsKey(selectedUid))
            {
                SuperController.singleton.RemoveAtom(lightControls[atomUidToGuid[selectedUid]].light.containingAtom);
            }
        }

        private UIDynamic UISpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
            return spacer;
        }

        private UIDynamicButton SelectLightButton(string uid, bool on)
        {
            UIDynamicButton uiButton = CreateButton(UI.LightButtonLabel(uid, on));
            uiButton.buttonColor = UI.black;
            uiButton.buttonText.alignment = TextAnchor.MiddleLeft;
            uiButton.button.onClick.AddListener(OnSelectLight(uid));
            return uiButton;
        }

        private UnityAction OnSelectLight(string uid)
        {
            return () =>
            {
                if(selectedUid != uid)
                {
                    RefreshUI(uid);
                }
                else
                {
                    ToggleLightOn(uid);
                }
            };
        }

        private void RefreshUI(string uid)
        {
            //Log.Message($"RefreshUI: selectedUid {selectedUid}, uid {uid}");
            if(atomUidToGuid.ContainsKey(selectedUid))
            {
                DestroyLightControlUI(selectedUid);
            }

            selectedUid = uid;
            if(!atomUidToGuid.ContainsKey(uid))
            {
                removeLightButton.button.interactable = false;
                return;
            }

            CreateLightControlUI(uid);
            removeLightButton.button.interactable = true;
        }

        private void DestroyLightControlUI(string uid)
        {
            //Log.Message($"DestroyLightControlUI: uid {uid}");
            try
            {
                LightControl lc = lightControls[atomUidToGuid[uid]];

                lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val);
                lc.SetOnColor(UI.lightGray);

                if(colorPickerSpacer != null)
                {
                    RemoveSpacer(colorPickerSpacer);
                }
                if(lightColorPicker != null)
                {
                    RemoveColorPicker(lightColorPicker);
                }
                if(selectTargetButton != null)
                {
                    RemoveButton(selectTargetButton);
                }
                if(enableLookAtToggle != null)
                {
                    RemoveToggle(enableLookAtToggle);
                }
                if(autoIntensityToggle != null)
                {
                    RemoveToggle(autoIntensityToggle);
                }
                if(autoRangeToggle != null)
                {
                    RemoveToggle(autoRangeToggle);
                }
                if(autoSpotAngleToggle != null)
                {
                    RemoveToggle(autoSpotAngleToggle);
                }
                if(lightTypeSpacer != null)
                {
                    RemoveSpacer(lightTypeSpacer);
                }
                if(lightTypePopup != null)
                {
                    RemovePopup(lightTypePopup);
                }
                if(intensitySlider != null)
                {
                    RemoveSlider(intensitySlider);
                }
                if(rangeSlider != null)
                {
                    RemoveSlider(rangeSlider);
                }
                if(spotAngleSlider != null)
                {
                    RemoveSlider(spotAngleSlider);
                }
                if(shadowStrengthSlider != null)
                {
                    RemoveSlider(shadowStrengthSlider);
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void CreateLightControlUI(string uid)
        {
            //Log.Message($"CreateLightControlUI: uid {uid}");
            LightControl lc = lightControls[atomUidToGuid[uid]];
            lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val, true);

            colorPickerSpacer = UISpacer(10f);
            lightColorPicker = CreateColorPicker(lc.lightColor);
            lightColorPicker.label = "Light color";
            selectTargetButton = CreateButton(UI.SelectTargetButtonLabel(lc.GetTargetString()), true);
            selectTargetButton.height = 100f;
            enableLookAtToggle = CreateToggle(lc.enableLookAt, true);
            autoIntensityToggle = CreateToggle(lc.autoIntensity, true);
            autoRangeToggle = CreateToggle(lc.autoRange, true);
            autoSpotAngleToggle = CreateToggle(lc.autoSpotAngle, true);

            lightTypeSpacer = UISpacer(10f, true);
            lightTypePopup = CreatePopup(lc.lightType, true);
            intensitySlider = CreateSlider(lc.intensity, true);
            intensitySlider.valueFormat = "F3";
            intensitySlider.label = "Intensity";
            rangeSlider = CreateSlider(lc.range, true);
            rangeSlider.label = "Range";
            spotAngleSlider = CreateSlider(lc.spotAngle, true);
            spotAngleSlider.label = "Spot angle";
            shadowStrengthSlider = CreateSlider(lc.shadowStrength, true);
            shadowStrengthSlider.valueFormat = "F3";
            shadowStrengthSlider.label = "Shadow strength";

            selectTargetButton.button.onClick.AddListener(() => StartCoroutine(lc.OnSelectTarget((targetString) =>
            {
                //target was null or same as before
                if(targetString == null)
                {
                    return;
                }

                UpdateAtomUID(lc);
                selectTargetButton.label = UI.SelectTargetButtonLabel(targetString);
            })));

            lc.lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange((value) =>
            {
                //lightType was same as before
                if(value == lc.prevLightType)
                {
                    return;
                }
                lc.prevLightType = value;
                UpdateAtomUID(lc);
            });

            lc.SetInteractableElements();
            lc.AddInteractableListeners();
        }

        private void UpdateAtomUID(LightControl lc)
        {
            SuperController.singleton.RenameAtom(
                lc.light.containingAtom,
                GenerateUID(lc.lightType.val, lc.GetButtonLabelTargetString())
            );
        }

        private void ToggleLightOn(string uid)
        {
            if(atomUidToGuid.ContainsKey(uid))
            {
                LightControl lc = lightControls[atomUidToGuid[uid]];
                lc.on.val = !lc.on.val;
                lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val, true);
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

            if(!atom.on || atom.type != atomType || atom.uid.StartsWith(atomUidPrefix) || atomUidToGuid.ContainsKey(atom.uid))
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
                Log.Message($"New {atomType} '{atom.uid}' was automatically disabled because 'Disable other point and spot lights' is checked in plugin UI.");
            }
        }

        private void OnRemoveAtom(Atom atom)
        {
            //light atom added by plugin was removed elsewhere
            string uid = atom.uid;
            if(atomUidToGuid.ContainsKey(uid))
            {
                DestroyLightControlUI(uid);

                string guid = atomUidToGuid[uid];
                LightControl lc = lightControls[guid];
                lightControls.Remove(guid);
                atomUidToGuid.Remove(uid);
                RemoveButton(lc.uiButton);
                Destroy(lc);
                RefreshUI(atomUidToGuid?.Keys.FirstOrDefault() ?? "");
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
            if(atomUidToGuid.ContainsKey(fromuid))
            {
                string guid = atomUidToGuid[fromuid];
                atomUidToGuid.Remove(fromuid);
                atomUidToGuid.Add(touid, guid);

                LightControl lc = lightControls[guid];
                bool selected = selectedUid == fromuid;
                lc.uiButton.label = UI.LightButtonLabel(touid, lc.on.val, selected);
                lc.uiButton.button.onClick.RemoveAllListeners();
                lc.uiButton.button.onClick.AddListener(OnSelectLight(touid));
                if(selected)
                {
                    selectedUid = touid;
                }
            }

            //selected light's target controller's containingAtom was renamed
            if(atomUidToGuid.ContainsKey(selectedUid))
            {
                LightControl selectedLc = lightControls[atomUidToGuid[selectedUid]];
                if(selectedLc != null && selectedLc.target != null && selectedLc.target.containingAtom.uid == touid)
                {
                    selectTargetButton.label = UI.SelectTargetButtonLabel(selectedLc.GetTargetString());
                }
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["lightControls"] = new JSONArray();
            lightControls.Values.ToList().ForEach(lc => json["lightControls"].Add(lc.Serialize()));
            if(selectedUid != "")
            {
                json["selected"] = selectedUid;
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
            RefreshUI(json["selected"]?.Value ?? "");

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

                //duplicated from AddExistingILAtomToPlugin
                lc.InitFromJson(atom, lightJson);
                string uid = atom.uid;
                lc.uiButton = SelectLightButton(uid, lc.on.val);
                string guid = Guid.NewGuid().ToString();
                atomUidToGuid.Add(uid, guid);
                lightControls.Add(guid, lc);
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
                if(atomUidToGuid != null && atomUidToGuid.ContainsKey(selectedUid))
                {
                    lightControls[atomUidToGuid[selectedUid]].SetOnColor(UITransform.gameObject.activeInHierarchy ? UI.turquoise : UI.lightGray);
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

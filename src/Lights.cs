using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Lumination
{
    internal class Lights : MVRScript
    {
        private Log log = new Log(nameof(Lights));
        private FreeControllerV3 control;

        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();
        private SortedDictionary<string, string> atomUidToGuid = new SortedDictionary<string, string>();
        private string selectedUid = "";

        private UIDynamicButton dupSelectedLightButton;
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
        private UIDynamicSlider pointBiasSlider;
        private UIDynamicSlider shadowStrengthSlider;
        private UIDynamicSlider distanceFromTargetSlider;

        private bool? restoringFromJson;
        private bool? removedFromPluginUI;
        private bool uiOpenPrevFrame = false;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "SubScene")
                {
                    log.Error($"Add to a SubsScene atom, not {containingAtom.type}.");
                    return;
                }

                control = containingAtom.freeControllers.First();
                control.SetBoolParamValue("freezeAtomPhysicsWhenGrabbed", false);

                InitUILeft();
                SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomParentChangedHandlers += new SuperController.OnAtomParentChanged(OnChangeAtomParent);
                SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnRenameAtom);
                IEnumerable<Atom> subSceneAtoms = containingAtom.subSceneComponent.atomsInSubScene;
                if(!containingAtom.uid.StartsWith(Const.SUBSCENE_UID))
                {
                    RenameSubScene(subSceneAtoms);
                }

                StartCoroutine(AddILAtomsInSubScene(subSceneAtoms, (uid) => RefreshUI(uid)));
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void InitUILeft()
        {
            AddSpotlightButton();
            AddPointlightButton();
            AddLightFromSceneButton();
            DupSelectedLightButton();
            RemoveLightButton();
            UISpacer(10);
        }

        private void AddSpotlightButton()
        {
            UIDynamicButton uiButton = CreateButton("Add new spotlight");
            uiButton.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => AddNewInvisibleLight("Spot"));
        }

        private void AddPointlightButton()
        {
            UIDynamicButton uiButton = CreateButton("Add new point light");
            uiButton.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => AddNewInvisibleLight("Point"));
        }

        private void AddLightFromSceneButton()
        {
            UIDynamicButton uiButton = CreateButton("Add light from scene");
            uiButton.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => AddInvisibleLightFromScene());
        }

        private void DupSelectedLightButton()
        {
            dupSelectedLightButton = CreateButton("Add copy of selected light atom");
            dupSelectedLightButton.buttonColor = UI.lightGreen;
            dupSelectedLightButton.button.onClick.AddListener(() => AddCopyOfSelectedLight());
        }

        private void RemoveLightButton()
        {
            removeLightButton = CreateButton("Remove selected light atom");
            removeLightButton.buttonColor = UI.pink;
            removeLightButton.button.onClick.AddListener(() => RemoveSelectedInvisibleLight());
        }

        private void RenameSubScene(IEnumerable<Atom> subSceneAtoms)
        {
            bool containsNonILAtoms = subSceneAtoms
                .Where(atom => atom.type != Const.INVLIGHT).ToList()
                .Count > 0;
            if(containsNonILAtoms)
            {
                log.Message($"Not renamed.");
                return;
            }

            SuperController.singleton.RenameAtom(containingAtom, Tools.NewUID(Const.SUBSCENE_UID));
        }

        private IEnumerator AddILAtomsInSubScene(IEnumerable<Atom> subSceneAtoms, Action<string> callback)
        {
            yield return new WaitForEndOfFrame();

            while(restoringFromJson.GetValueOrDefault(false))
            {
                yield return null;
            }

            subSceneAtoms
                .Where(atom => atom.type == Const.INVLIGHT && !atomUidToGuid.ContainsKey(atom.uid))
                .OrderBy(atom => atom.uid).ToList()
                .ForEach(atom =>
                {
                    if(MaxLights())
                    {
                        log.Error($"Failed to add {atom.uid} to plugin!");
                    }
                    else
                    {
                        JSONStorable light = atom.GetStorableByID("Light");
                        string lightType = light.GetStringChooserParamValue("type");
                        if(lightType != "Point" && lightType != "Spot")
                        {
                            return;
                        }

                        LightControl lc = gameObject.AddComponent<LightControl>();
                        lc.Init(atom.GetStorableByID("Light"), FindControlFromSubScene(atom.uid), lightType);
                        UpdateAtomUID(lc);
                        AddLightControlToPlugin(lc, atom.uid);
                    }
                });

            if(restoringFromJson == null)
            {
                callback(atomUidToGuid?.Keys.FirstOrDefault() ?? "");
            }
        }

        private void AddNewInvisibleLight(string lightType)
        {
            if(MaxLights())
            {
                return;
            }

            StartCoroutine(Tools.CreateAtomCo(Const.INVLIGHT, Tools.NewUID(lightType), (atom) =>
            {
                if(lightType == "Spot")
                {
                    atom.transform.forward = Vector3.down;
                }

                atom.parentAtom = containingAtom; //add atom to subscene

                LightControl lc = gameObject.AddComponent<LightControl>();
                lc.Init(atom.GetStorableByID("Light"), FindControlFromSubScene(atom.uid), lightType);
                AddLightControlToPlugin(lc, atom.uid);
                if(lightType == "Spot")
                {
                    lc.intensity.val = 1.2f;
                    lc.range.val = 7;
                }
                RefreshUI(atom.uid);
            }));
        }

        private void AddInvisibleLightFromScene()
        {
            if(MaxLights())
            {
                return;
            }

            try
            {
                SuperController.singleton.SelectModeControllers(
                    new SuperController.SelectControllerCallback(selectedCtrl =>
                    {
                        Atom atom = selectedCtrl.containingAtom;
                        if(atom.type != Const.INVLIGHT)
                        {
                            log.Message($"Selected atom is not an {Const.INVLIGHT} atom!");
                            return;
                        }

                        if(atomUidToGuid.ContainsKey(atom.uid))
                        {
                            log.Message($"Selected {Const.INVLIGHT} is already added!");
                            return;
                        }

                        JSONStorable light = atom.GetStorableByID("Light");
                        string lightType = light.GetStringChooserParamValue("type");

                        if(!LightControl.types.Contains(lightType))
                        {
                            log.Message("Only Spot and Point lights are supported.");
                            return;
                        }

                        if(!atom.on)
                        {
                            atom.ToggleOn();
                        }

                        atom.parentAtom = containingAtom; //add atom to subscene
                        light.SetBoolParamValue("on", true);

                        LightControl lc = gameObject.AddComponent<LightControl>();
                        lc.Init(atom.GetStorableByID("Light"), selectedCtrl, lightType);
                        UpdateAtomUID(lc);
                        AddLightControlToPlugin(lc, atom.uid);

                        SuperController.singleton.SelectController(control);
                        RefreshUI(atom.uid);
                    })
                );
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void AddCopyOfSelectedLight()
        {
            if(MaxLights())
            {
                return;
            }

            try
            {
                LightControl sourceLc = lightControls[atomUidToGuid[selectedUid]];
                string lightType = sourceLc.lightType.val;
                Atom sourceAtom = sourceLc.light.containingAtom;

                JSONClass sceneJSON = SuperController.singleton.GetSaveJSON(sourceAtom, true, true);
                JSONClass sourceAtomJSON = sceneJSON["atoms"].AsArray[0].AsObject;
                JSONClass sourceLcJSON = sourceLc.Serialize();

                StartCoroutine(Tools.CreateAtomCo(Const.INVLIGHT, Tools.NewUID(lightType), (atom) =>
                {
                    atom.PreRestore();
                    atom.Restore(sourceAtomJSON, true, true, true);
                    atom.LateRestore(sourceAtomJSON, true, true, true);
                    atom.PostRestore();
                    atom.GetComponentInChildren<Transform>().Translate(0.1f, 0, 0.1f);

                    atom.parentAtom = containingAtom; //add atom to subscene

                    LightControl lc = gameObject.AddComponent<LightControl>();
                    lc.InitFromJson(atom.GetStorableByID("Light"), FindControlFromSubScene(atom.uid), sourceLcJSON);
                    UpdateAtomUID(lc);
                    AddLightControlToPlugin(lc, atom.uid);
                    RefreshUI(atom.uid);
                }));
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private bool MaxLights()
        {
            if(lightControls.Count >= 6)
            {
                log.Error("You have the maximum number of lights.");
                return true;
            }

            return false;
        }

        private string ParseBasename(string uid)
        {
            try
            {
                string excludeNum = Regex.Split(uid, "#\\d")[0];
                string excludeSubscene = Regex.Split(excludeNum, $"{containingAtom.uid}/")[1];
                return excludeSubscene;
            }
            catch(Exception)
            {
                return uid;
            }
        }

        private void AddLightControlToPlugin(LightControl lc, string uid)
        {
            lc.uiButton = SelectLightButton(uid, lc.on.val);
            string guid = Guid.NewGuid().ToString();
            atomUidToGuid.Add(uid, guid);
            lightControls.Add(guid, lc);
        }

        //must get control from subscene's children because the atom parented to subscene doesn't have a child FreeControllerV3
        private FreeControllerV3 FindControlFromSubScene(string uid)
        {
            return containingAtom
                .GetComponentsInChildren<FreeControllerV3>()
                .Where(it => it.containingAtom.uid == uid)
                .First();
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
            if(atomUidToGuid.ContainsKey(selectedUid))
            {
                DestroyLightControlUI(selectedUid);
            }

            selectedUid = uid;
            if(!atomUidToGuid.ContainsKey(uid))
            {
                dupSelectedLightButton.button.interactable = false;
                removeLightButton.button.interactable = false;
                return;
            }

            LightControl lc = lightControls[atomUidToGuid[uid]];
            lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val, true);
            lc.SetTransformIconStyle(UITransform.gameObject.activeInHierarchy);

            CreateLightControlUI(lc);
            PostCreateLightControlUI(lc);
            dupSelectedLightButton.button.interactable = true;
            removeLightButton.button.interactable = true;
        }

        private void DestroyLightControlUI(string uid)
        {
            LightControl lc = lightControls[atomUidToGuid[uid]];

            lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val);
            lc.SetTransformIconStyle();

            if(colorPickerSpacer != null)
                RemoveSpacer(colorPickerSpacer);
            if(lightColorPicker != null)
                RemoveColorPicker(lightColorPicker);
            if(selectTargetButton != null)
                RemoveButton(selectTargetButton);
            if(enableLookAtToggle != null)
                RemoveToggle(enableLookAtToggle);
            if(autoRangeToggle != null)
                RemoveToggle(autoRangeToggle);
            if(autoIntensityToggle != null)
                RemoveToggle(autoIntensityToggle);
            if(autoSpotAngleToggle != null)
                RemoveToggle(autoSpotAngleToggle);
            if(distanceFromTargetSlider != null)
                RemoveSlider(distanceFromTargetSlider);

            if(lightTypeSpacer != null)
                RemoveSpacer(lightTypeSpacer);
            if(lightTypePopup != null)
                RemovePopup(lightTypePopup);
            if(rangeSlider != null)
                RemoveSlider(rangeSlider);
            if(intensitySlider != null)
                RemoveSlider(intensitySlider);
            if(spotAngleSlider != null)
                RemoveSlider(spotAngleSlider);
            if(pointBiasSlider != null)
                RemoveSlider(pointBiasSlider);
            if(shadowStrengthSlider != null)
                RemoveSlider(shadowStrengthSlider);
        }

        private void CreateLightControlUI(LightControl lc)
        {
            colorPickerSpacer = UISpacer(CalculateLeftSpacerHeight());
            lightColorPicker = CreateColorPicker(lc.lightColor);
            lightColorPicker.label = "Light color";

            selectTargetButton = CreateButton(UI.SelectTargetButtonLabel(lc.GetTargetString()), true);
            selectTargetButton.height = 115;
            enableLookAtToggle = CreateToggle(lc.enableLookAt, true);
            autoRangeToggle = CreateToggle(lc.autoRange, true);
            autoIntensityToggle = CreateToggle(lc.autoIntensity, true);
            autoSpotAngleToggle = CreateToggle(lc.autoSpotAngle, true);
            distanceFromTargetSlider = CreateSlider(lc.distanceFromTarget, true);
            distanceFromTargetSlider.valueFormat = "F3";
            distanceFromTargetSlider.label = "Distance from Target";
            lc.SetSliderClickMonitor(distanceFromTargetSlider.slider.gameObject.AddComponent<PointerStatus>());

            lightTypeSpacer = UISpacer(10, true);
            lightTypePopup = CreatePopup(lc.lightType, true);
            rangeSlider = CreateSlider(lc.range, true);
            rangeSlider.label = "Range";
            intensitySlider = CreateSlider(lc.intensity, true);
            intensitySlider.valueFormat = "F3";
            intensitySlider.label = "Intensity";
            if(lc.lightType.val == "Spot")
            {
                spotAngleSlider = CreateSlider(lc.spotAngle, true);
                spotAngleSlider.label = "Spot angle";
            }
            else if(lc.lightType.val == "Point")
            {
                pointBiasSlider = CreateSlider(lc.pointBias, true);
                pointBiasSlider.valueFormat = "F3";
                pointBiasSlider.label = "Point bias";
            }
            shadowStrengthSlider = CreateSlider(lc.shadowStrength, true);
            shadowStrengthSlider.valueFormat = "F3";
            shadowStrengthSlider.label = "Shadow strength";
        }

        private void PostCreateLightControlUI(LightControl lc)
        {
            selectTargetButton.button.onClick.AddListener(() => StartCoroutine(lc.OnSelectTarget((targetString) =>
            {
                SuperController.singleton.SelectController(control);
                UpdateAtomUID(lc);
                selectTargetButton.label = UI.SelectTargetButtonLabel(targetString);
            })));

            lc.lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange((value) =>
            {
                //refresh UI to swap between spot angle and point bias sliders
                if(lc.prevLightType != value)
                {
                    RefreshUI(lc.light.containingAtom.uid);
                }
                lc.prevLightType = value;
                UpdateAtomUID(lc);
            });

            lc.UpdateInteractablesAndStyles();
            lc.AddListeners();
        }

        private void UpdateAtomUID(LightControl lc)
        {
            Atom atom = lc.light.containingAtom;
            string aimAt = lc.GetButtonLabelTargetString();
            string basename = lc.lightType.val + (string.IsNullOrEmpty(aimAt) ? "" : $"-{aimAt}");
            if(basename == ParseBasename(atom.uid))
            {
                return; //prevent rename of atom if only the number sequence differs
            }
            atom.SetUID(Tools.NewUID(basename));
        }

        //aligns color picker to the plugin UI lower edge based on the number of spotlights
        private float CalculateLeftSpacerHeight()
        {
            float btnSpacerHeight = 65;
            return 682 - (5 * btnSpacerHeight + 10 + (lightControls.Count - 1) * btnSpacerHeight);
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

        private void OnEnable()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => it.enabled = true);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        #region SuperController Listeners

        private void OnRemoveAtom(Atom atom)
        {
            //light atom controlled by the plugin was removed
            string uid = atom.uid;
            if(atomUidToGuid.ContainsKey(uid))
            {
                RemoveLightControl(uid);
            }

            //atom targeted by a light was removed
            lightControls.Values
                .Where(lc => lc.GetTargetUID() == uid).ToList()
                .ForEach(lc =>
                {
                    //sets the removed atom's freeController to null, should automatically be removed later on anyway
                    //(just to avoid a coroutine here)
                    lc.target = null;
                    lc.hasTarget = false;
                    UpdateAtomUID(lc);
                    if(lc.light.containingAtom.uid == selectedUid)
                    {
                        selectTargetButton.label = UI.SelectTargetButtonLabel(lc.GetTargetString());
                    }
                });
        }

        private void OnChangeAtomParent(Atom atom, Atom newParent)
        {
            //light atom controlled by the plugin was unparented from subscene
            if(atomUidToGuid.ContainsKey(atom.uid) && (newParent == null || newParent.uid != containingAtom.uid))
            {
                RemoveLightControl(atom.uid);
            }
        }

        private void RemoveLightControl(string uid)
        {
            if(selectedUid == uid)
            {
                DestroyLightControlUI(uid);
            }

            string guid = atomUidToGuid[uid];
            LightControl lc = lightControls[guid];
            lightControls.Remove(guid);
            atomUidToGuid.Remove(uid);
            RemoveButton(lc.uiButton);
            colorPickerSpacer.height = CalculateLeftSpacerHeight();
            Destroy(lc);

            if(selectedUid == uid)
            {
                RefreshUI(atomUidToGuid?.Keys.FirstOrDefault() ?? "");
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

        #endregion SuperController Listeners

        #region JSON

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["lightControls"] = new JSONArray();
            lightControls.Values.ToList().ForEach(lc => json["lightControls"].Add(lc.Serialize()));
            if(selectedUid != "")
            {
                json["selected"] = selectedUid;
            }
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

            RefreshUI(json["selected"]?.Value ?? "");

            restoringFromJson = false;
        }

        private void RestoreLightControlFromJSON(JSONClass lightJson)
        {
            string atomUid = lightJson["atomUid"].Value;
            Atom atom = GetAtomById(atomUid);
            if(atom == null)
            {
                log.Message($"Unable to control light atom '{atomUid}': mentioned in saved JSON but not found in scene.");
                return;
            }

            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.InitFromJson(atom.GetStorableByID("Light"), FindControlFromSubScene(atomUid), lightJson);
            AddLightControlToPlugin(lc, atom.uid);
        }

        #endregion JSON

        public void UpdateEnableLookAtUIToggle(bool hasTarget, bool isSpot)
        {
            ApplyToggleStyle(enableLookAtToggle, hasTarget && isSpot);
        }

        public void UpdateAutoRangeUIToggle(bool hasTarget)
        {
            ApplyToggleStyle(autoRangeToggle, hasTarget);
        }

        public void UpdateAutoIntensityUIToggle(bool hasTarget, bool autoRangeVal)
        {
            ApplyToggleStyle(autoIntensityToggle, hasTarget && autoRangeVal);
        }

        public void UpdateAutoSpotAngleUIToggle(bool hasTarget, bool isSpot)
        {
            ApplyToggleStyle(autoSpotAngleToggle, hasTarget && isSpot);
        }

        private void ApplyToggleStyle(UIDynamicToggle uiToggle, bool val)
        {
            bool on = uiToggle.toggle.isOn;
            uiToggle.textColor = val ? UI.black : (on ? UI.offGrayRed : UI.gray);
            uiToggle.backgroundColor = val ? UI.white : (on ? UI.lightPink : UI.white);
        }

        private void Update()
        {
            try
            {
                bool uiOpen = UITransform.gameObject.activeInHierarchy;
                if(uiOpen == uiOpenPrevFrame)
                {
                    return;
                }

                if(uiOpen)
                {
                    atomUidToGuid?.Where(kvp => kvp.Key != selectedUid).ToList().ForEach(kvp =>
                    {
                        LightControl lc = lightControls[kvp.Value];
                        lc.uiButton.label = UI.LightButtonLabel(kvp.Key, lc.on.val);
                    });
                }

                if(atomUidToGuid != null && atomUidToGuid.ContainsKey(selectedUid))
                {
                    LightControl selectedLc = lightControls[atomUidToGuid[selectedUid]];
                    selectedLc.SetTransformIconStyle(uiOpen);
                    selectedLc.uiButton.label = UI.LightButtonLabel(selectedUid, selectedLc.on.val, true);
                }

                uiOpenPrevFrame = uiOpen;
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void OnDisable()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => it.enabled = false);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => Destroy(it));
                SuperController.singleton.onAtomRemovedHandlers -= new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomParentChangedHandlers -= new SuperController.OnAtomParentChanged(OnChangeAtomParent);
                SuperController.singleton.onAtomUIDRenameHandlers -= new SuperController.OnAtomUIDRename(OnRenameAtom);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }
    }
}

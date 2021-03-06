using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

namespace Lumination
{
    internal class Lights : MVRScript
    {
        private Log log = new Log(nameof(Lights));
        private FreeControllerV3 control;

        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();
        private SortedDictionary<string, string> atomUidToGuid = new SortedDictionary<string, string>();
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
            AddLightFromSceneButton();
            RemoveLightButton();
            UISpacer(10);
        }

        private void AddSpotlightButton()
        {
            UIDynamicButton uiButton = CreateButton("Add new spotlight");
            uiButton.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => AddNewInvisibleLight());
        }

        private void AddLightFromSceneButton()
        {
            UIDynamicButton uiButton = CreateButton("Add light from scene");
            uiButton.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => AddSelectedInvisibleLight());
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
                    JSONStorable light = atom.GetStorableByID("Light");
                    string lightType = light.GetStringChooserParamValue("type");
                    if(lightType != "Point" && lightType != "Spot")
                    {
                        return;
                    }

                    AddExistingILAtomToPlugin(atom, lightType, true);
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
                log.Message("You have the maximum number of lights.");
                return;
            }

            StartCoroutine(Tools.CreateAtomCo(Const.INVLIGHT, Tools.NewUID("Spot"), (atom) =>
            {
                atom.transform.forward = Vector3.down;
                atom.parentAtom = containingAtom; //add atom to subscene
                AddExistingILAtomToPlugin(atom, "Spot", false, (lc) =>
                {
                    lc.intensity.val = 1.2f;
                    lc.range.val = 7;
                });
                RefreshUI(atom.uid);
            }));
        }

        private void AddSelectedInvisibleLight()
        {
            if(lightControls.Count >= 6)
            {
                log.Message("You have the maximum number of lights.");
                return;
            }

            try
            {
                SuperController.singleton.SelectModeControllers(
                    new SuperController.SelectControllerCallback(targetCtrl =>
                    {
                        Atom atom = targetCtrl.containingAtom;
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

                        light.SetBoolParamValue("on", true);
                        AddExistingILAtomToPlugin(atom, lightType, true);
                        RefreshUI(atom.uid);
                    })
                );
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
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

        private void AddExistingILAtomToPlugin(Atom atom, string lightType, bool updateUid, Action<LightControl> callback = null)
        {
            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.Init(atom.GetStorableByID("Light"), FindControlFromSubScene(atom.uid), lightType);
            if(updateUid)
            {
                UpdateAtomUID(lc); //ensure name is correct when reloading plugin
            }
            lc.uiButton = SelectLightButton(atom.uid, lc.on.val);
            string guid = Guid.NewGuid().ToString();
            atomUidToGuid.Add(atom.uid, guid);
            lightControls.Add(guid, lc);

            if(callback != null)
            {
                callback(lc);
            }
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
            //log.Message($"RefreshUI: selectedUid {selectedUid}, uid {uid}");
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
            //log.Message($"DestroyLightControlUI: uid {uid}");
            try
            {
                LightControl lc = lightControls[atomUidToGuid[uid]];

                lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val);
                lc.SetOnStyle();

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
                if(autoRangeToggle != null)
                {
                    RemoveToggle(autoRangeToggle);
                }
                if(autoIntensityToggle != null)
                {
                    RemoveToggle(autoIntensityToggle);
                }
                if(autoSpotAngleToggle != null)
                {
                    RemoveToggle(autoSpotAngleToggle);
                }
                if(distanceFromTargetSlider != null)
                {
                    RemoveSlider(distanceFromTargetSlider);
                }

                if(lightTypeSpacer != null)
                {
                    RemoveSpacer(lightTypeSpacer);
                }
                if(lightTypePopup != null)
                {
                    RemovePopup(lightTypePopup);
                }
                if(rangeSlider != null)
                {
                    RemoveSlider(rangeSlider);
                }
                if(intensitySlider != null)
                {
                    RemoveSlider(intensitySlider);
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
                log.Error($"{e}");
            }
        }

        private void CreateLightControlUI(string uid)
        {
            //log.Message($"CreateLightControlUI: uid {uid}");
            LightControl lc = lightControls[atomUidToGuid[uid]];
            lc.uiButton.label = UI.LightButtonLabel(uid, lc.on.val, true);
            lc.SetOnStyle(UITransform.gameObject.activeInHierarchy);

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
            spotAngleSlider = CreateSlider(lc.spotAngle, true);
            spotAngleSlider.label = "Spot angle";
            shadowStrengthSlider = CreateSlider(lc.shadowStrength, true);
            shadowStrengthSlider.valueFormat = "F3";
            shadowStrengthSlider.label = "Shadow strength";

            selectTargetButton.button.onClick.AddListener(() => StartCoroutine(lc.OnSelectTarget((targetString) =>
            {
                SuperController.singleton.SelectController(control);
                UpdateAtomUID(lc);
                selectTargetButton.label = UI.SelectTargetButtonLabel(targetString);
            })));

            lc.lightType.popup.onValueChangeHandlers += new UIPopup.OnValueChange((value) =>
            {
                lc.prevLightType = value;
                UpdateAtomUID(lc);
            });

            lc.SetInteractableElements();
            lc.AddInteractableListeners();
            lc.AddAutoToggleListeners();
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
            float buttonHeight = 50;
            float buttonSpacerHeight = 15;
            return 477 - (lightControls.Count - 1) * (buttonHeight + buttonSpacerHeight);
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

        private void OnRemoveAtom(Atom atom)
        {
            //light atom controlled by the plugin was removed
            string uid = atom.uid;
            if(atomUidToGuid.ContainsKey(uid))
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

            //atom targeted by a light was removed
            lightControls.Values
                .Where(lc => lc.targetUid == uid).ToList()
                .ForEach(lc =>
                {
                    //sets the removed atom's freeController to null, should automatically be removed later on anyway
                    //(just to avoid a coroutine here)
                    lc.target = null;
                    lc.targetUid = null;
                    UpdateAtomUID(lc);
                    if(lc.light.containingAtom.uid == selectedUid)
                    {
                        selectTargetButton.label = UI.SelectTargetButtonLabel(lc.GetTargetString());
                    }
                });
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

            //duplicated from AddExistingILAtomToPlugin
            lc.InitFromJson(atom.GetStorableByID("Light"), FindControlFromSubScene(atomUid), lightJson);
            lc.uiButton = SelectLightButton(atomUid, lc.on.val);
            string guid = Guid.NewGuid().ToString();
            atomUidToGuid.Add(atomUid, guid);
            lightControls.Add(guid, lc);
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
                    selectedLc.SetOnStyle(uiOpen);
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
                SuperController.singleton.onAtomUIDRenameHandlers -= new SuperController.OnAtomUIDRename(OnRenameAtom);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }
    }
}

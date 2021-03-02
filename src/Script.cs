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
        private const string atomType = "InvisibleLight";
        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();

        private JSONStorableBool disableOtherLights;
        private List<Atom> disabledLights = new List<Atom>();

        private string selectedUid = "";

        private UIDynamicButton removeLightButton;
        private UIDynamic leftUISpacer;

        private UIDynamicButton selectTargetButton;

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

            UIDynamicButton addSpotLightButton = CreateButton("Add new spotlight");
            addSpotLightButton.buttonColor = UI.lightGreen;
            addSpotLightButton.button.onClick.AddListener(() => AddNewInvisibleLight());
            UI.DecreaseAvailableHeight(addSpotLightButton.height);

            UIDynamicButton addLightFromSceneButton = CreateButton("Add light from scene");
            addLightFromSceneButton.buttonColor = UI.lightGreen;
            addLightFromSceneButton.button.onClick.AddListener(() => AddSelectedInvisibleLight());
            UI.DecreaseAvailableHeight(addLightFromSceneButton.height);

            removeLightButton = CreateButton("Remove selected atom");
            removeLightButton.buttonColor = UI.pink;
            removeLightButton.button.onClick.AddListener(() => RemoveSelectedInvisibleLight());
            UI.DecreaseAvailableHeight(removeLightButton.height);

            DisableOtherLightsUIToggle();

            UISpacer(15f);

            leftUISpacer = CreateSpacer();
            leftUISpacer.height = UI.GetAvailableHeight();
        }

        private IEnumerator AddExistingILAtoms(Action<string> callback)
        {
            yield return new WaitForEndOfFrame();

            while(restoringFromJson)
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

                    AddExistingILAtomToPlugin(atom, $"{light.type}");
                });

            callback(lightControls?.Keys.FirstOrDefault() ?? "");
        }

        private void TitleUITextField()
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable);
            field.backgroundColor = UI.defaultPluginBgColor;
            field.textColor = UI.white;
            field.UItext.alignment = TextAnchor.MiddleCenter;
            field.height = 100;
            UI.DecreaseAvailableHeight(field.height);
            storable.val = UI.Size("\n", 24) + UI.Bold(UI.Size($"{nameof(Illumination)} {version}", 36));
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

            StartCoroutine(Tools.CreateAtomCo(atomType, $"{atomUidPrefix}{atomType}", (atom) =>
            {
                string uid = AddExistingILAtomToPlugin(atom, "Spot");
                RefreshUI(uid);
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
                        if(atom.type != atomType)
                        {
                            Log.Message($"Selected atom is not an {atomType} atom!");
                            return;
                        }

                        if(lightControls.ContainsKey(atom.uid))
                        {
                            Log.Message($"Selected {atomType} is already added!");
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
                        string uid = AddExistingILAtomToPlugin(atom, lightType);
                        RefreshUI(uid);
                    })
                );
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private string AddExistingILAtomToPlugin(Atom atom, string lightType)
        {
            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.Init(atom, lightType);
            string uid = atom.uid;
            lc.uiButton = UILightButton(uid);
            lightControls.Add(uid, lc);
            return uid;
        }

        private void RemoveSelectedInvisibleLight()
        {
            //if(lightControls != null && lightControls.ContainsKey(selected))
            if(lightControls.ContainsKey(selectedUid))
            {
                SuperController.singleton.RemoveAtom(lightControls[selectedUid].light.containingAtom);
            }
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
            UIDynamicButton uiButton = CreateButton(UI.LightButtonLabel(uid));
            UI.DecreaseAvailableHeight(uiButton.height);
            uiButton.buttonColor = UI.black;
            uiButton.buttonText.alignment = TextAnchor.MiddleLeft;
            leftUISpacer = CreateSpacer();
            leftUISpacer.height = UI.GetAvailableHeight();
            uiButton.button.onClick.AddListener(() => {
                RefreshUI(uid);
            });
            return uiButton;
        }

        private void RefreshUI(string uid)
        {
            if(lightControls.ContainsKey(selectedUid))
            {
                DestroyLightControlUI(selectedUid);
            }

            selectedUid = uid;
            if(!lightControls.ContainsKey(uid))
            {
                RemoveButton(selectTargetButton);
                removeLightButton.button.interactable = false;
                return;
            }

            CreateLightControlUI(uid);
        }

        private void DestroyLightControlUI(string uid)
        {
            LightControl lc = lightControls[uid];

            lc.uiButton.label = UI.LightButtonLabel(uid);
            lc.SetOnColor(UI.lightGray);

            UI.IncreaseAvailableHeight(380);
            leftUISpacer.height += 380 + 15;
            RemoveColorPicker(lc.lightColor);

            selectTargetButton?.button.onClick.RemoveAllListeners();

            RemoveToggle(lc.enableLookAt);
            RemoveToggle(lc.autoIntensity);
            RemoveToggle(lc.autoRange);
            RemoveToggle(lc.autoSpotAngle);
            RemoveSlider(lc.intensity);
            RemoveSlider(lc.range);
            RemovePopup(lc.lightType);
            RemoveSlider(lc.spotAngle);
            RemoveSlider(lc.shadowStrength);
        }

        private void CreateLightControlUI(string uid)
        {
            LightControl lc = lightControls[uid];
            lc.uiButton.label = UI.LightButtonLabel(uid, true);

            CreateColorPicker(lc.lightColor);
            UI.DecreaseAvailableHeight(380);
            leftUISpacer.height -= 380 + 15;

            if(selectTargetButton == null)
            {
                selectTargetButton = CreateButton(UI.SelectTargetButtonLabel(""), true);
                selectTargetButton.height = 100;
            }
            else
            {
                selectTargetButton.label = UI.SelectTargetButtonLabel(lc.GetTargetString());
            }

            CreateToggle(lc.enableLookAt, true);
            CreateToggle(lc.autoIntensity, true);
            CreateToggle(lc.autoRange, true);
            CreateToggle(lc.autoSpotAngle, true);
            CreateSlider(lc.intensity, true);
            CreateSlider(lc.range, true);
            CreatePopup(lc.lightType, true);
            CreateSlider(lc.spotAngle, true);
            CreateSlider(lc.shadowStrength, true);

            selectTargetButton.button.onClick.AddListener(() => StartCoroutine(lc.OnSelectTarget((targetString) =>
            {
                selectTargetButton.label = UI.SelectTargetButtonLabel(targetString);
                lc.enableLookAt.toggle.interactable = true;
            })));

            lc.enableLookAt.toggle.interactable = lc.target != null;

            removeLightButton.button.interactable = true;
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

            if(!atom.on || atom.type != atomType || atom.uid.StartsWith(atomUidPrefix) || lightControls.ContainsKey(atom.uid))
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
            if(lightControls.ContainsKey(atom.uid))
            {
                DestroyLightControlUI(atom.uid);

                LightControl lc = lightControls[atom.uid];
                lightControls.Remove(atom.uid);
                UI.IncreaseAvailableHeight(lc.uiButton.height);
                RemoveButton(lc.uiButton);
                leftUISpacer.height = UI.GetAvailableHeight();
                Destroy(lc);
                RefreshUI(lightControls?.Keys.FirstOrDefault() ?? "");
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
                if(selectedUid == fromuid)
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

                //duplicated from AddExistingILAtomToPlugin
                lc.InitFromSave(atom, target, lightJson["enableLookAt"].AsBool);
                string uid = atom.uid;
                lc.uiButton = UILightButton(uid);
                lightControls.Add(uid, lc);
                RefreshUI(uid);
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
                if(lightControls != null && lightControls.ContainsKey(selectedUid))
                {
                    lightControls[selectedUid].SetOnColor(UITransform.gameObject.activeInHierarchy ? UI.turquoise : UI.lightGray);
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

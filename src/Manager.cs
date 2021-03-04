using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Illumination
{
    internal class Manager : MVRScript
    {
        private Log log = new Log(nameof(Manager));
        private const string version = "<Version>";

        private JSONStorableBool enablePositionParentLink;
        private string disableOtherLightsLabel = "Disable other point and spot lights";
        private JSONStorableBool disableOtherLights;
        private Dictionary<string, Atom> disabledLights = new Dictionary<string, Atom>();

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                InitUILeft();
                InitUIRight();

                SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomAddedHandlers += new SuperController.OnAtomAdded(OnAddAtom);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        private void InitUILeft()
        {
            TitleUITextField($"{nameof(Illumination)} {version}");

            UISpacer(15);
            LoadButton();
            SaveButton();
            LoadDefaultButton();
            SaveDefaultButton();
            ClearButton();

            UISpacer(15);
            SelectTargetButton();
            UnsetTargetButton();
            PositionParentLinkToggle();

            UISpacer(75);
            DisableOtherLightsToggle();
        }

        private void InitUIRight()
        {
            UISpacer(130, true);
            string presets = $"A preset includes all controlled light atoms and their settings, as well as the below Target and Other settings." +
                $"{UI.LineBreak()}The default preset is automatically loaded when the plugin is added.";
            var presetsField = UsageUITextField("presets", "Lighting Rig Presets", presets);
            presetsField.height = 310;

            UISpacer(15, true);
            string rigTarget = $"Select a target from the scene to center the lighting rig around." +
                $"{UI.LineBreak()}Presets using a target missing from the scene will center around (0,0,0)." +
                $"{UI.LineBreak()}Enable parent linking to keep lights aligned to a moving target.";
            var rigTargetField = UsageUITextField("rigTarget", "Lighting Rig Target", rigTarget);
            rigTargetField.height = 310;

            UISpacer(15, true);
            string other = "Turns off Spot and Point type invisible lights not controlled by Illumination while the plugin is active.";
            var otherField = UsageUITextField("other", "Other Settings", other);
            otherField.height = 310;
        }

        private void TitleUITextField(string title, bool rightSide = false)
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable, rightSide);
            field.backgroundColor = UI.defaultPluginBgColor;
            field.textColor = UI.white;
            field.UItext.alignment = TextAnchor.MiddleCenter;
            field.height = 100;
            storable.val = UI.TitleTextStyle(title);
        }

        private void LoadButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Load", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void LoadDefaultButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Load Default", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SaveButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Save As", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SaveDefaultButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Save As Default", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void ClearButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Clear", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SelectTargetButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Select and align to Target", rightSide);
            uiButton.height = 120;
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void UnsetTargetButton(bool rightSide = false)
        {
            UIDynamicButton uiButton = CreateButton("Unset Target", rightSide);
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void PositionParentLinkToggle(bool rightSide = false)
        {
            enablePositionParentLink = new JSONStorableBool("Parent link position to Target Atom", false);
            UIDynamicToggle uiToggle = CreateToggle(enablePositionParentLink, rightSide);
            enablePositionParentLink.toggle.onValueChanged.AddListener(val => { });
        }

        private void DisableOtherLightsToggle(bool rightSide = false)
        {
            disableOtherLights = new JSONStorableBool("Switch off other lights", false);
            UIDynamicToggle uiToggle = CreateToggle(disableOtherLights, rightSide);
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

        private UIDynamicTextField UsageUITextField(string name, string title, string text)
        {
            JSONStorableString storable = new JSONStorableString(name, UI.FormatUsage(title, text));
            UIDynamicTextField field = CreateTextField(storable, true);
            //field.backgroundColor = UI.defaultPluginBgColor;
            return field;
        }

        private void UISpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        private void DisableOtherPointAndSpotLights()
        {
            GetSceneAtoms().ForEach(atom => DisableAtomIfIsOtherLight(atom));
        }

        private bool DisableAtomIfIsOtherLight(Atom atom)
        {
            if(!atom.on || disableOtherLights == null || !disableOtherLights.val)
            {
                return false;
            }

            if(atom.type != Const.ATOM_TYPE || atom.uid.StartsWith(Const.UID_PREFIX))
            {
                return false;
            }

            JSONStorable light = atom.GetStorableByID("Light");
            string lightType = light.GetStringChooserJSONParam("type").val;
            if(lightType == "Point" || lightType == "Spot")
            {
                light.SetBoolParamValue("on", false);
                disabledLights.Add(atom.uid, atom);
                return true;
            }

            return false;
        }

        private void EnableDisabledLights()
        {
            disabledLights?.Values.ToList().ForEach(atom =>
            {
                if(atom.on)
                {
                    JSONStorable light = atom.GetStorableByID("Light");
                    light.SetBoolParamValue("on", true);
                }
            });
        }

        private void OnAddAtom(Atom atom)
        {
            bool wasDisabled = DisableAtomIfIsOtherLight(atom);
            if(wasDisabled)
            {
                log.Message($"New {Const.ATOM_TYPE} '{atom.uid}' was automatically disabled because '{disableOtherLightsLabel}' is checked in plugin UI.");
            }
        }

        private void OnRemoveAtom(Atom atom)
        {
            if(disabledLights.ContainsKey(atom.uid))
            {
                disabledLights.Remove(atom.uid);
            }
        }

        private void OnEnable()
        {
            try
            {
                DisableOtherPointAndSpotLights();
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["enablePositionParentLink"].AsBool = enablePositionParentLink.val;
            json["disableOtherLights"].AsBool = disableOtherLights.val;
            json["disabledLights"] = new JSONArray();
            disabledLights.Keys.ToList().ForEach(uid => json["disabledLights"].Add(uid));
            needsStore = true;
            return json;
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(RestoreFromJSONInternal(json));
        }

        private IEnumerator RestoreFromJSONInternal(JSONClass json)
        {
            yield return new WaitForEndOfFrame();

            enablePositionParentLink.val = json["enablePositionParentLink"].AsBool;
            disableOtherLights.val = json["disableOtherLights"].AsBool;
            foreach(JSONNode node in json["disabledLights"].AsArray)
            {
                string uid = node.Value;
                if(!disabledLights.ContainsKey(uid))
                {
                    disabledLights.Add(uid, GetAtomById(uid));
                }
            }

            DisableOtherPointAndSpotLights();
        }

        private void OnDisable()
        {
            try
            {
                EnableDisabledLights();
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
                SuperController.singleton.onAtomAddedHandlers -= new SuperController.OnAtomAdded(OnAddAtom);
                SuperController.singleton.onAtomRemovedHandlers -= new SuperController.OnAtomRemoved(OnRemoveAtom);
            }
            catch(Exception e)
            {
                log.Error($"{e}");
            }
        }
    }
}

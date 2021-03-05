using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lumination
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
                if(containingAtom.type != "SubScene")
                {
                    log.Error($"Load to a SubsScene Atom, not {containingAtom.type}.");
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
            TitleUITextField($"{nameof(Lumination)} {version}");

            UISpacer(15);
            LoadButton();
            SaveButton();
            LoadDefaultButton();
            SaveDefaultButton();
            AutoAlignToggle();

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
            string presets = $"A preset includes all controlled light atoms and their settings." +
                $"{UI.LineBreak()}The default preset is automatically loaded when the plugin is added to the scene.";
            var presetsField = UsageUITextField("presets", "Lighting Rig Presets", presets);
            presetsField.height = 310;

            UISpacer(15, true);
            string rigTarget = $"Select a target from the scene to center the lighting rig around." +
                $"{UI.LineBreak()}Following makes the lights parent linked to the target atom.";
            var rigTargetField = UsageUITextField("rigTarget", "Lighting Rig Target", rigTarget);
            rigTargetField.height = 310;

            UISpacer(15, true);
            string other = $"Turns off Spot and Point type invisible lights not in this SubScene while {nameof(Lumination)} is active.";
            var otherField = UsageUITextField("other", "Other Settings", other);
            otherField.height = 310;
        }

        private void TitleUITextField(string title)
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable);
            field.backgroundColor = UI.defaultPluginBgColor;
            field.textColor = UI.white;
            field.UItext.alignment = TextAnchor.MiddleCenter;
            field.height = 100;
            storable.val = UI.TitleTextStyle(title);
        }

        private void LoadButton()
        {
            UIDynamicButton uiButton = CreateButton("Load preset");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void LoadDefaultButton()
        {
            UIDynamicButton uiButton = CreateButton("Load default preset");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SaveButton()
        {
            UIDynamicButton uiButton = CreateButton("Save preset");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SaveDefaultButton()
        {
            UIDynamicButton uiButton = CreateButton("Save current as default");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void AutoAlignToggle()
        {
            JSONStorableBool storable = new JSONStorableBool("Auto-align to Person", true);
            UIDynamicToggle uiToggle = CreateToggle(storable);
            storable.toggle.onValueChanged.AddListener(val => { });
        }

        private void SelectTargetButton()
        {
            UIDynamicButton uiButton = CreateButton("Select Target and align to");
            uiButton.height = 120;
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void UnsetTargetButton()
        {
            UIDynamicButton uiButton = CreateButton("Unset Target");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void PositionParentLinkToggle()
        {
            enablePositionParentLink = new JSONStorableBool("Follow Target atom", false);
            UIDynamicToggle uiToggle = CreateToggle(enablePositionParentLink);
            enablePositionParentLink.toggle.onValueChanged.AddListener(val => { });
        }

        private void DisableOtherLightsToggle()
        {
            disableOtherLights = new JSONStorableBool("Switch off other lights", false);
            UIDynamicToggle uiToggle = CreateToggle(disableOtherLights);
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

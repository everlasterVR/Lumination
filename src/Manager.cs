using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lumination
{
    //mostly deprecated code
    internal class Manager : MVRScript
    {
        private const string version = "v0.0.0";

        private JSONStorableBool enablePositionParentLink;
        private string switchOffOtherLightsLabel = "Switch off other point and spot lights";
        private JSONStorableBool switchOffOtherLights;
        private Dictionary<string, Atom> switchedOffLights = new Dictionary<string, Atom>();

        public override void Init()
        {
            try
            {
                InitUILeft();
                InitUIRight();

                SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomAddedHandlers += new SuperController.OnAtomAdded(OnAddAtom);
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(Manager));
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
            switchOffOtherLights = new JSONStorableBool("Switch off other lights", false);
            UIDynamicToggle uiToggle = CreateToggle(switchOffOtherLights);
            switchOffOtherLights.toggle.onValueChanged.AddListener(val =>
            {
                if(val)
                {
                    SwitchOffOtherLights();
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

        private void SwitchOffOtherLights()
        {
            GetSceneAtoms().ForEach(atom => SwitchOffIfNotInSubscene(atom));
        }

        private bool SwitchOffIfNotInSubscene(Atom atom)
        {
            if(!atom.on || switchOffOtherLights == null || !switchOffOtherLights.val)
            {
                return false;
            }

            if(atom.type != Const.INVLIGHT)
            {
                return false;
            }

            //atom in current subscene
            if(atom.containingSubScene != null && atom.containingSubScene.containingAtom.uid == containingAtom.uid)
            {
                return false;
            }

            JSONStorable light = atom.GetStorableByID("Light");
            string lightType = light.GetStringChooserJSONParam("type").val;
            if(lightType == "Point" || lightType == "Spot")
            {
                light.SetBoolParamValue("on", false);
                switchedOffLights.Add(atom.uid, atom);
                return true;
            }

            return false;
        }

        private void EnableDisabledLights()
        {
            switchedOffLights?.Values.ToList().ForEach(atom =>
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
            bool wasDisabled = SwitchOffIfNotInSubscene(atom);
            if(wasDisabled)
            {
                Log.Message($"New {Const.INVLIGHT} '{atom.uid}' was automatically disabled because '{switchOffOtherLightsLabel}' is checked in plugin UI.", nameof(Manager));
            }
        }

        private void OnRemoveAtom(Atom atom)
        {
            if(switchedOffLights.ContainsKey(atom.uid))
            {
                switchedOffLights.Remove(atom.uid);
            }
        }

        private void OnEnable()
        {
            try
            {
                SwitchOffOtherLights();
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(Manager));
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["enablePositionParentLink"].AsBool = enablePositionParentLink.val;
            json["switchOffOtherLights"].AsBool = switchOffOtherLights.val;
            json["switchedOffLights"] = new JSONArray();
            switchedOffLights.Keys.ToList().ForEach(uid => json["switchedOffLights"].Add(uid));
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
            switchOffOtherLights.val = json["switchOffOtherLights"].AsBool;
            foreach(JSONNode node in json["switchedOffLights"].AsArray)
            {
                string uid = node.Value;
                if(!switchedOffLights.ContainsKey(uid))
                {
                    switchedOffLights.Add(uid, GetAtomById(uid));
                }
            }

            SwitchOffOtherLights();
        }

        private void OnDisable()
        {
            try
            {
                EnableDisabledLights();
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(Manager));
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
                Log.Error($"{e}", nameof(Manager));
            }
        }
    }
}

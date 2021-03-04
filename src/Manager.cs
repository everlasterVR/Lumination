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
                //InitUIRight();

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
            TitleUITextField();
            LoadLightingRigButton();
            SaveLightingRigButton();
            ClearLightingRigButton();
            SelectCenterAtomButton();
            EnablePositionParentLinkToggle();
            DisableOtherLightsToggle();
        }

        //private void InitUIRight()
        //{

        //}

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

        private void LoadLightingRigButton()
        {
            UIDynamicButton uiButton = CreateButton("Load lighting rig");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SaveLightingRigButton()
        {
            UIDynamicButton uiButton = CreateButton("Save lighting rig");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void ClearLightingRigButton()
        {
            UIDynamicButton uiButton = CreateButton("Clear lighting rig");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void SelectCenterAtomButton()
        {
            UIDynamicButton uiButton = CreateButton("Select center atom");
            //button.buttonColor = UI.lightGreen;
            uiButton.button.onClick.AddListener(() => { });
        }

        private void EnablePositionParentLinkToggle()
        {
            enablePositionParentLink = new JSONStorableBool("Enable position parent link", false);
            UIDynamicToggle uiToggle = CreateToggle(enablePositionParentLink);
            uiToggle.height = 100f;
            enablePositionParentLink.toggle.onValueChanged.AddListener(val => { });
        }

        private void DisableOtherLightsToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot InvisibleLights", false);
            UIDynamicToggle uiToggle = CreateToggle(disableOtherLights);
            uiToggle.height = 100f;
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

            Light light = atom.GetComponentInChildren<Light>();
            if(light.type == LightType.Point || light.type == LightType.Spot)
            {
                atom.ToggleOn();
                disabledLights.Add(atom.uid, atom);
                return true;
            }

            return false;
        }

        private void EnableDisabledLights()
        {
            disabledLights?.Values.ToList().ForEach(atom =>
            {
                if(!atom.on)
                {
                    atom.ToggleOn();
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

            foreach(var item in disabledLights)
            {
                log.Message($"{item.Key} {item.Value.uid}");
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

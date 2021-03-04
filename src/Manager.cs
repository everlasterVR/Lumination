using System;
using System.Collections.Generic;
using UnityEngine;

namespace Illumination
{
    internal class Manager : MVRScript
    {
        private Log log = new Log(nameof(Manager));
        private const string version = "<Version>";

        private string disableOtherLightsLabel = "Disable other point and spot lights";
        private JSONStorableBool disableOtherLights;
        private List<Atom> disabledLights = new List<Atom>();

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
            JSONStorableBool storable = new JSONStorableBool("Enable position parent link", false);
            RegisterBool(storable);
            UIDynamicToggle uiToggle = CreateToggle(storable);
            storable.toggle.onValueChanged.AddListener(val => { });
        }

        private void DisableOtherLightsToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot lights", false);
            RegisterBool(disableOtherLights);
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

            if(atom.type != Const.ATOM_TYPE || atom.uid.StartsWith(Const.UID_PREFIX))
            {
                return false;
            }

            Light light = atom.GetComponentInChildren<Light>();
            if(light.type == LightType.Point || light.type == LightType.Spot)
            {
                if(atom.on)
                {
                    atom.ToggleOn();
                    disabledLights.Add(atom);
                    return true;
                }
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
            if(disabledLights.Contains(atom))
            {
                disabledLights.Remove(atom);
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

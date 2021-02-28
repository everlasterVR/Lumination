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

        private JSONStorableStringChooser lightUISelect;
        private JSONStorableString pointingAtInfo;
        private UIDynamicButton selectTargetButton;
        private UIDynamicButton stopPointingButton;
        private UIDynamicButton removeButton;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                TitleUITextField();
                CreateHairSelect();

                DisableOtherLightsUIToggle();

                UIDynamicButton addSpotLightButton = CreateButton("Add spot light");
                addSpotLightButton.button.onClick.AddListener(() => AddInvisibleLight(LightType.Spot));

                UIDynamicButton addPointLightButton = CreateButton("Add point light");
                addPointLightButton.button.onClick.AddListener(() => AddInvisibleLight(LightType.Point));

                selectTargetButton = CreateButton("Select target to point at", true);
                pointingAtInfo = new JSONStorableString("Pointing at info", "");
                UIDynamicTextField pointingAtInfoField = CreateTextField(pointingAtInfo, true);
                pointingAtInfoField.height = 100;

                stopPointingButton = CreateButton("Stop pointing", true);
                removeButton = CreateButton("Remove", true);

                AddSuperControllerOnAtomActions();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void TitleUITextField()
        {
            JSONStorableString storable = new JSONStorableString("title", "");
            UIDynamicTextField field = CreateTextField(storable);
            field.UItext.fontSize = 36;
            field.height = 100;
            storable.val = $"<b>{nameof(Illumination)}</b>\n<size=28>v{version}</size>";
        }

        private void CreateHairSelect()
        {
            lightUISelect = new JSONStorableStringChooser(
                "Light select",
                new List<string>(),
                "",
                "Selected",
                RefreshUI
            );
            UIDynamicPopup lightUISelectPopup = CreatePopup(lightUISelect, true);
            lightUISelectPopup.height = 100;
        }

        private void DisableOtherLightsUIToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot lights", false);
            UIDynamicToggle disableOtherLightsToggle = CreateToggle(disableOtherLights);
            disableOtherLights.toggle.onValueChanged.AddListener(val => {
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

        private void AddInvisibleLight(LightType lightType)
        {
            StartCoroutine(Tools.CreateAtomCo("InvisibleLight", $"{atomUidPrefix}{lightType}Light", (atom) =>
            {
                LightControl lc = gameObject.AddComponent<LightControl>();
                lc.Init(atom, lightType);
                lightControls.Add(atom.uid, lc);
                lightUISelect.choices.Add(atom.uid);
                lightUISelect.val = atom.uid;
            }));
        }

        private void RefreshUI(string key)
        {
            selectTargetButton.button.onClick.RemoveAllListeners();
            stopPointingButton.button.onClick.RemoveAllListeners();
            removeButton.button.onClick.RemoveAllListeners();

            if(lightControls.ContainsKey(key))
            {
                LightControl lc = lightControls[key];
                UpdateInfo(lc.GetAimConstrainTargetName());

                selectTargetButton.button.onClick.AddListener(() =>
                {
                    lc.OnSelectTarget();
                    StartCoroutine(AwaitUpdateInfo(lc));
                });

                stopPointingButton.button.onClick.AddListener(() =>
                {
                    lc.OnStopPointing();
                    UpdateInfo(null);
                });

                removeButton.button.onClick.AddListener(() => {
                    lightControls.Remove(key);
                    Destroy(lc);
                    lightUISelect.choices.Remove(key);
                    lightUISelect.val = lightUISelect.choices.FirstOrDefault() ?? "";
                });
            }
            else
            {
                UpdateInfo(null);
            }
        }

        private void UpdateInfo(string targetName)
        {
            if(targetName == null)
            {
                pointingAtInfo.val = $"";
                return;
            }

            pointingAtInfo.val = $"Pointing at: {targetName}";
        }

        private IEnumerator AwaitUpdateInfo(LightControl lc)
        {
            while(lc.GetAimConstrainTargetName() == null)
            {
                pointingAtInfo.val = $"";
                yield return null;
            }

            pointingAtInfo.val = $"Pointing at: {lc.GetAimConstrainTargetName()}";
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
            SuperController.singleton.GetAtoms().ForEach(atom => DisableAtomIfIsOtherLight(atom));
        }

        private bool DisableAtomIfIsOtherLight(Atom atom)
        {
            if(disableOtherLights == null || !disableOtherLights.val)
            {
                return false;
            }

            if(!atom.enabled || atom.type != "InvisibleLight" || atom.uid.StartsWith(atomUidPrefix) || lightControls.ContainsKey(atom.uid))
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
            disabledLights?.ForEach(atom => {
                if(!atom.on)
                {
                    atom.ToggleOn();
                }
            });
        }

        private void AddSuperControllerOnAtomActions()
        {
            SuperController.singleton.onAtomAddedHandlers += new SuperController.OnAtomAdded(OnAtomAdd);
            SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnAtomRemove);
            SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnAtomRename);
        }

        private void OnAtomAdd(Atom atom)
        {
            bool wasDisabled = DisableAtomIfIsOtherLight(atom);
            if(wasDisabled)
            {
                Log.Message($"New InvisibleLight '{atom.uid}' was automatically disabled because 'Disable other point and spot lights' is checked in plugin UI.");
            }
        }

        private void OnAtomRemove(Atom atom)
        {
            //light atom added by plugin was removed elsewhere
            if(lightControls.ContainsKey(atom.uid))
            {
                LightControl lc = lightControls[atom.uid];
                lightControls.Remove(atom.uid);
                Destroy(lc);
                bool isSelected = lightUISelect.val == atom.uid;
                lightUISelect.choices.Remove(atom.uid);
                if(isSelected)
                {
                    lightUISelect.val = lightUISelect.choices.FirstOrDefault() ?? "";
                }
            }
            //other light atom disabled by plugin was removed
            if(disabledLights.Contains(atom))
            {
                disabledLights.Remove(atom);
            }
        }

        private void OnAtomRename(string fromuid, string touid)
        {
            //light atom added by plugin was renamed elsewhere
            if(lightControls.ContainsKey(fromuid))
            {
                LightControl lc = lightControls[fromuid];
                lightControls.Remove(fromuid);
                lightControls.Add(touid, lc);
                bool isSelected = lightUISelect.val == fromuid;
                lightUISelect.choices.Remove(fromuid);
                lightUISelect.choices.Add(touid);
                if(isSelected)
                {
                    lightUISelect.val = touid;
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

        //public void Update()
        //{
        //    try
        //    {
        //    }
        //    catch(Exception e)
        //    {
        //        Log.Error($"{e}");
        //    }
        //}

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
                SuperController.singleton.onAtomAddedHandlers -= new SuperController.OnAtomAdded(OnAtomAdd);
                SuperController.singleton.onAtomRemovedHandlers -= new SuperController.OnAtomRemoved(OnAtomRemove);
                SuperController.singleton.onAtomUIDRenameHandlers -= new SuperController.OnAtomUIDRename(OnAtomRename);
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

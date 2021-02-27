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
        private Dictionary<string, LightControl> lightControls;

        JSONStorableStringChooser lightUISelect;
        JSONStorableString pointingAtInfo;
        UIDynamicButton selectTargetButton;
        UIDynamicButton stopPointingButton;
        UIDynamicButton removeButton;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                lightControls = new Dictionary<string, LightControl>();

                TitleUITextField();
                CreateHairSelect();

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
                lightControls.Keys.ToList(),
                "",
                "Selected",
                RefreshUI
            );
            UIDynamicPopup lightUISelectPopup = CreatePopup(lightUISelect, true);
            lightUISelectPopup.height = 100;
        }

        private void AddInvisibleLight(LightType lightType)
        {
            StartCoroutine(Tools.CreateAtomCo("InvisibleLight", $"Illum_{lightType}Light", (atom) =>
            {
                LightControl lc = gameObject.AddComponent<LightControl>();
                lc.Init(atom, lightType);
                lightControls.Add(atom.uid, lc);
                lightUISelect.choices = lightControls.Keys.ToList();
                lightUISelect.val = lightUISelect.choices.First();
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
                    lightUISelect.choices = lightControls.Keys.ToList();
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
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
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
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

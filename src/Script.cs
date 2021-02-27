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
        private List<LightControl> lightControls;

        UIDynamicButton selectTargetButton;
        UIDynamicButton stopPointingButton;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                lightControls = new List<LightControl>();

                TitleUITextField();

                UIDynamicButton addSpotLightButton = CreateButton("Add spot light");
                addSpotLightButton.button.onClick.AddListener(() => StartCoroutine(AddInvisibleLight(LightType.Spot)));

                UIDynamicButton addPointLightButton = CreateButton("Add point light");
                addPointLightButton.button.onClick.AddListener(() => StartCoroutine(AddInvisibleLight(LightType.Point)));

                selectTargetButton = CreateButton("Select target to point at", true);
                stopPointingButton = CreateButton("Stop pointing", true);
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

        private IEnumerator AddInvisibleLight(LightType lightType)
        {
            string atomUid = NewAtomUid(lightType);
            yield return SuperController.singleton.AddAtomByType("InvisibleLight", atomUid);
            StartCoroutine(InitLightControl(lightType, atomUid));
        }

        private IEnumerator InitLightControl(LightType lightType, string atomUid)
        {
            Atom newLight = null;
            while(newLight == null)
            {
                newLight = SuperController.singleton.GetAtomByUid(atomUid);
                yield return null;
            }

            LightControl lc = gameObject.AddComponent<LightControl>();
            lc.Init(newLight, lightType);
            lightControls.Add(lc);

            // TODO switch active lightControl in UI
            selectTargetButton.button.onClick.RemoveAllListeners();
            selectTargetButton.button.onClick.AddListener(lc.OnSelectTarget);

            stopPointingButton.button.onClick.RemoveAllListeners();
            stopPointingButton.button.onClick.AddListener(lc.OnStopPointing);
        }

        public void OnEnable()
        {
            try
            {
                lightControls?.ForEach(it => it.enabled = true);
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private string NewAtomUid(LightType lightType)
        {
            string uid = "Illum_";
            switch(lightType)
            {
                case LightType.Spot:
                    uid += "SpotLight";
                    break;
                case LightType.Point:
                    uid += "PointLight";
                    break;
                default:
                    break;
            }

            int count = lightControls.Where(lc => lc.atom.uid.StartsWith($"{uid}")).Count();

            return $"{uid}#{count + 1}";
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
                lightControls?.ForEach(it => it.enabled = false);
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
                lightControls?.ForEach(it => Destroy(it));
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

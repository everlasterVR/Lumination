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
                StartCoroutine(InitLightControls());
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

        private IEnumerator InitLightControls()
        {
            lightControls = new List<LightControl>();

            yield return new WaitForEndOfFrame();

            SuperController.singleton.GetAtoms()
                .Where(atom => atom.type == "InvisibleLight").ToList()
                .ForEach(atom =>
                {
                    LightControl lc = gameObject.AddComponent<LightControl>();
                    lc.Init(atom);
                    lightControls.Add(lc);
                });

            SelectTargetUIButton();
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

        private void SelectTargetUIButton()
        {
            //TODO generalize
            LightControl lc = lightControls.First();

            UIDynamicButton selectTargetButton = CreateButton("Select target");
            selectTargetButton.button.onClick.AddListener(lc.OnSelectTarget);
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

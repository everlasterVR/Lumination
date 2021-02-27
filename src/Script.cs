using System;
using System.Collections.Generic;
using System.Linq;

namespace Illumination
{
    internal class Script : MVRScript
    {
        private const string version = "<Version>";

        private AimConstrain aimConstrain;

        public override void Init()
        {
            try
            {
                if(containingAtom.type != "CoreControl")
                {
                    Log.Error($"Must be loaded as a Scene Plugin.");
                    return;
                }

                if(gameObject.GetComponent<AimConstrain>() == null)
                {
                    List<Atom> atoms = SuperController.singleton.GetAtoms();
                    List<Atom> lightAtoms = atoms.Where(atom => atom.type == "InvisibleLight").ToList();
                    // TODO add to some other gameObject than containingAtom?
                    aimConstrain = gameObject.AddComponent<AimConstrain>();
                    // TODO select from list
                    aimConstrain.Init(lightAtoms.First());
                }

                TitleUITextField();
                SelectTargetUIButton();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        public void OnEnable()
        {
            try
            {
                if(aimConstrain != null)
                {
                    aimConstrain.enabled = true;
                }
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

        private void SelectTargetUIButton()
        {
            UIDynamicButton selectTargetButton = CreateButton("Select target");
            selectTargetButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.SelectModeControllers(
                    new SuperController.SelectControllerCallback(target => aimConstrain.SetTarget(target))
                );
            });
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
                if(aimConstrain != null)
                {
                    aimConstrain.enabled = false;
                }
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
                Destroy(gameObject.GetComponent<AimConstrain>());
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

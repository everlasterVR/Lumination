using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        private Atom light;
        private AimConstrain aimConstrain;

        public void Init(Atom light)
        {
            this.light = light;
        }

        public void OnSelectTarget()
        {
            if(aimConstrain == null)
            {
                aimConstrain = new AimConstrain(light);
            }

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(target => aimConstrain.SetTarget(target))
            );
        }

        private void OnEnable()
        {
            if(aimConstrain != null)
            {
                aimConstrain.SetConstraintActive(true);
            }
        }

        private void OnDisable()
        {
            if(aimConstrain != null)
            {
                aimConstrain.SetConstraintActive(false);
            }
        }
    }
}

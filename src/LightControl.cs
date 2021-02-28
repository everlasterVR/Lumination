using UnityEngine;

namespace Illumination
{
    internal class LightControl : MonoBehaviour
    {
        public Atom atom;
        private AimConstrain aimConstrain;

        public void Init(Atom atom, LightType lightType)
        {
            this.atom = atom;

            // init defaults
            Light light = atom.GetComponentInChildren<Light>();
            light.type = lightType;

            FreeControllerV3 fc = atom.gameObject.GetComponentInChildren<FreeControllerV3>();
            fc.physicsEnabled = true;
            fc.onColor = new Color(1f, 1f, 1f, 0.5f);
            fc.highlighted = false;
        }

        public void OnSelectTarget()
        {
            if(aimConstrain == null)
            {
                aimConstrain = gameObject.AddComponent<AimConstrain>();
                aimConstrain.Init(atom);
            }

            SuperController.singleton.SelectModeControllers(
                new SuperController.SelectControllerCallback(target => aimConstrain.SetTarget(target))
            );
        }

        public void OnStopPointing()
        {
            Destroy(aimConstrain);
            aimConstrain = null;
        }

        public string GetAimConstrainTargetName()
        {
            return aimConstrain?.GetTargetName();
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

        private void OnDestroy()
        {
            Destroy(aimConstrain);
            SuperController.singleton.RemoveAtom(atom);
        }
    }
}

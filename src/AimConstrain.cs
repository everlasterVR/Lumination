using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Illumination
{
    /*
     * LICENSE: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0) https://creativecommons.org/licenses/by-nc-sa/4.0/
     * Adapted from prestigitis_aimConstrain.cs by prestigitis (CC BY-NC-SA 4.0)
     */

    public class AimConstrain : MonoBehaviour
    {
        private ConstraintSource cs;
        private FreeControllerV3 sourceCtrl;
        public FreeControllerV3 targetCtrl;

        public void Init(Atom parentAtom)
        {
            sourceCtrl = parentAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
        }

        public void SetTarget(FreeControllerV3 target)
        {
            targetCtrl = target;
            AddAimConstraintTargetingTransform();
        }

        private void AddAimConstraintTargetingTransform()
        {
            cs.sourceTransform = targetCtrl.transform;
            cs.weight = 1;

            if(sourceCtrl == null)
            {
                Log.Message("AimConstrain Component needs to be on an Atom with an active FreeControllerV3. Make sure Atom is not parented.", nameof(AimConstrain));
                return;
            }

            AimConstraint ac = sourceCtrl.gameObject.GetComponent<AimConstraint>();
            if(ac == null)
            {
                ac = sourceCtrl.gameObject.AddComponent(typeof(AimConstraint)) as AimConstraint;
            }

            for(int i = 0; i < ac.sourceCount; i++)
            {
                ac.RemoveSource(i);
            }
            ac.AddSource(cs);

            ac.aimVector.Set(0, 0, 1);
            ac.upVector.Set(0, 1, 0);
            ac.worldUpType = AimConstraint.WorldUpType.None;
            ac.weight = 1;
            ac.rotationAxis = Axis.X | Axis.Y; //change this to constrain axis
            ac.constraintActive = true;
            return;
        }

        public AimConstraint GetAimConstraint()
        {
            FreeControllerV3 fc = sourceCtrl.gameObject.GetComponentInChildren<FreeControllerV3>();
            return fc.gameObject.GetComponent<AimConstraint>();
        }

        public void SetConstraintActive(bool value)
        {
            try
            {
                AimConstraint ac = sourceCtrl?.gameObject.GetComponent<AimConstraint>();

                if(ac != null)
                {
                    ac.constraintActive = value;
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        private void OnDestroy()
        {
            Destroy(sourceCtrl?.gameObject.GetComponent<AimConstraint>());
        }
    }
}
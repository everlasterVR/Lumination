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
        private Atom aimingAtom;
        private FreeControllerV3 target;

        public void Init(Atom aimingAtom)
        {
            this.aimingAtom = aimingAtom;
        }

        public void SetTarget(FreeControllerV3 target)
        {
            //Log.Message($"Adding AimConstraint to {aimingAtom.name}, aiming at {target.name}", nameof(AimConstrain));
            this.target = target;
            AddAimConstraintTargetingTransform();
        }

        //adds an aimconstraint to the containing atom, targeting the targetXForm
        private void AddAimConstraintTargetingTransform()
        {
            //set up constraint source using target transform
            cs.sourceTransform = target.transform;
            cs.weight = 1;

            //set up aimconstraint component on the freecontroller of containing atom
            FreeControllerV3 fc = aimingAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            if(fc == null) //make sure containing atom has a FCV3
            {
                Log.Message("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.", nameof(AimConstrain));
                return;
            }

            AimConstraint ac = fc.gameObject.GetComponent<AimConstraint>();
            if(ac == null)
            {
                //there is no aimconstraint component yet, so create a new aimconstraint
                ac = fc.gameObject.AddComponent(typeof(AimConstraint)) as AimConstraint;
            }

            //reset source
            for(int i = 0; i < ac.sourceCount; i++)
            {
                ac.RemoveSource(i);
            }
            ac.AddSource(cs);

            //set other aimconstraint parameters
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
            FreeControllerV3 fc = aimingAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            return fc.gameObject.GetComponent<AimConstraint>();
        }

        public string GetTargetName()
        {
            return target?.name;
        }

        public void SetConstraintActive(bool value)
        {
            try
            {
                FreeControllerV3 fc = aimingAtom?.gameObject.GetComponentInChildren<FreeControllerV3>();
                AimConstraint ac = fc?.gameObject.GetComponent<AimConstraint>();

                if(ac != null)
                {
                    ac.constraintActive = value;
                    //Log.Message($"Successfully set constraintActive={value} for {aimingAtom.name}", nameof(AimConstrain));
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        private void OnDestroy()
        {
            FreeControllerV3 fc = aimingAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            Destroy(fc?.gameObject.GetComponent<AimConstraint>());
        }
    }
}
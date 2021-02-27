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
        protected ConstraintSource cs;
        private Atom aimingAtom;

        public void Init(Atom aimingAtom, Atom targetAtom)
        {
            this.aimingAtom = aimingAtom;
            Log.Message($"Adding AimConstraint to {aimingAtom.name}, aiming at {targetAtom.uid}", nameof(AimConstrain));
            AddAimConstraintTargetingTransform(targetAtom.gameObject.GetComponentInChildren<FreeControllerV3>().transform);
        }

        private void OnEnable()
        {
            SetConstraintActive(true);
        }

        //adds an aimconstraint to the containing atom, targeting the targetXForm
        private void AddAimConstraintTargetingTransform(Transform targetXForm)
        {
            //set up constraint source using target transform
            cs.sourceTransform = targetXForm;
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
            RemoveSources(ac);
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

        public void DisableAimConstraint() //removes all the constraint sources
        {
            Log.Message($"Removing AimConstraint from {aimingAtom.name}", nameof(AimConstrain));
            FreeControllerV3 fc = aimingAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
            if(fc == null) //make sure containing atom has a FCV3
            {
                Log.Message("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.", nameof(AimConstrain));
                return;
            }
            AimConstraint ac = fc.gameObject.GetComponent<AimConstraint>();
            if(ac != null)
            {
                RemoveSources(ac);
                ac.constraintActive = false;
            }
        }

        private void OnDisable()
        {
            SetConstraintActive(false);
        }

        private void SetConstraintActive(bool value)
        {
            try
            {
                FreeControllerV3 fc = aimingAtom?.gameObject.GetComponentInChildren<FreeControllerV3>();
                AimConstraint ac = fc?.gameObject.GetComponent<AimConstraint>();

                if(ac != null)
                {
                    ac.constraintActive = value;
                    Log.Message($"Successfully set constraintActive={value} for {aimingAtom.name}", nameof(AimConstrain));
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        //remove all constraint sources
        private void RemoveSources(AimConstraint ac)
        {
            for(int i = 0; i < ac.sourceCount; i++)
            {
                ac.RemoveSource(i);
            }
        }
    }
}
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

        private void OnDisable()
        {
            SetConstraintActive(false);
        }

        private void SetConstraintActive(bool value)
        {
            try
            {
                if(aimingAtom != null)
                {
                    FreeControllerV3 fc = aimingAtom.gameObject.GetComponentInChildren<FreeControllerV3>();
                    if(fc != null)
                    {
                        if(fc.gameObject.GetComponent<AimConstraint>() != null)
                        {
                            fc.gameObject.GetComponent<AimConstraint>().constraintActive = value;
                            Log.Message($"Successfully set constraintActive={value} for {aimingAtom.name}", nameof(AimConstrain));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        private void AddAimConstraintTargetingTransform(Transform targetXForm) //adds an aimconstraint to the containing atom, targeting the targetXForm
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
            if(fc.gameObject.GetComponent<AimConstraint>() == null)
            {
                //there is no aimconstraint component yet, so create a new aimconstraint and add a new source
                AimConstraint ac = fc.gameObject.AddComponent(typeof(AimConstraint)) as AimConstraint;
            }
            if(fc.gameObject.GetComponent<AimConstraint>().sourceCount > 1)
            {
                //aimconstraint exists, but too many constraint sources, remove them all
                for(int i = 0; i < fc.gameObject.GetComponent<AimConstraint>().sourceCount; i++)
                {
                    fc.gameObject.GetComponent<AimConstraint>().RemoveSource(i);
                }
            }

            if(fc.gameObject.GetComponent<AimConstraint>().sourceCount == 1)
            {
                //use the existing constraint source
                fc.gameObject.GetComponent<AimConstraint>().SetSource(0, cs);
            }
            else
            {
                //no constraint sources, so add a new one
                fc.gameObject.GetComponent<AimConstraint>().AddSource(cs);
            }
            //set other aimconstraint parameters
            fc.gameObject.GetComponent<AimConstraint>().aimVector.Set(0, 0, 1);
            fc.gameObject.GetComponent<AimConstraint>().upVector.Set(0, 1, 0);
            fc.gameObject.GetComponent<AimConstraint>().worldUpType = AimConstraint.WorldUpType.None;
            fc.gameObject.GetComponent<AimConstraint>().weight = 1;
            fc.gameObject.GetComponent<AimConstraint>().rotationAxis = Axis.X | Axis.Y; //change this to constrain axis
            fc.gameObject.GetComponent<AimConstraint>().constraintActive = true;
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
            if(fc.gameObject.GetComponent<AimConstraint>() == null)
            {
                //there is no aimconstraint so do nothing
                return;
            }
            if(fc.gameObject.GetComponent<AimConstraint>().sourceCount > 0)
            {
                //remove all constraint sources
                for(int i = 0; i < fc.gameObject.GetComponent<AimConstraint>().sourceCount; i++)
                {
                    fc.gameObject.GetComponent<AimConstraint>().RemoveSource(i);
                    fc.gameObject.GetComponent<AimConstraint>().constraintActive = false;
                }
            }
        }
    }
}
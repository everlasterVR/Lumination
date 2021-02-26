using System;
using UnityEngine;
using UnityEngine.Animations;
using System.Collections.Generic;

namespace Illumination
{
    /*
     * LICENSE: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0) https://creativecommons.org/licenses/by-nc-sa/4.0/
     * Adapted from prestigitis_aimConstrain.cs by prestigitis (CC BY-NC-SA 4.0)
     */

    public class AimConstrain : MVRScript
    {
        protected ConstraintSource cs;
        protected Atom receivingAtom;
        protected JSONStorableStringChooser atomJSON;

        protected void SyncAtomChoices()
        {
            List<string> stringList = new List<string>();
            stringList.Add("None");
            foreach(string atomUiD in SuperController.singleton.GetAtomUIDs())
                if(SuperController.singleton.GetAtomByUid(atomUiD).gameObject.GetComponentInChildren<FreeControllerV3>() != null)
                {
                    //only add atoms that have an FCV3
                    stringList.Add(atomUiD);
                }
            atomJSON.choices = stringList;
        }

        protected void OnAtomRename(string oldid, string newid)
        {
            SyncAtomChoices();
            if(atomJSON == null || !(receivingAtom != null))
                return;
            atomJSON.valNoCallback = receivingAtom.uid;
        }

        protected void SyncAtomFreeControllers(string atomUID)
        {
            if(atomJSON.val == "None")
            {
                DisableAimConstraint();
            }
            else
            {
                AddAimConstraintTargetingTransform(SuperController.singleton.GetAtomByUid(atomJSON.val).gameObject.GetComponentInChildren<FreeControllerV3>().transform);
                if(!this.isActiveAndEnabled)
                {
                    //script is disabled, so disable aimconstraint
                    containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().constraintActive = false;
                }
            }
        }

        public override void Init()
        {
            try
            {
                // put init code in here
                if((bool) (SuperController.singleton))
                    SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnAtomRename);

                atomJSON = new JSONStorableStringChooser(
                  "atom", SuperController.singleton.GetAtomUIDs(), "None",
                  "Target Atom", new JSONStorableStringChooser.SetStringCallback(SyncAtomFreeControllers));

                atomJSON.popupOpenCallback = new JSONStorableStringChooser.PopupOpenCallback(SyncAtomChoices);

                RegisterStringChooser(atomJSON);

                CreateScrollablePopup(atomJSON, true);

                SyncAtomChoices(); //add atoms in the scene to the list of choices
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        void OnEnable()
        {
            try
            {
                if(containingAtom != null)
                {
                    if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>() != null)
                    {
                        if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>() != null)
                        {
                            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().constraintActive = true;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        void OnDisable()
        {
            try
            {
                if(containingAtom != null)
                {
                    if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>() != null)
                    {
                        if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>() != null)
                        {
                            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().constraintActive = false;
                        }
                    }
                }

            }
            catch(Exception e)
            {
                Log.Error($"{e}", nameof(AimConstrain));
            }
        }

        void AddAimConstraintTargetingTransform(Transform targetXForm) //adds an aimconstraint to the containing atom, targeting the targetXForm
        {
            //set up constraint source using target transform
            cs.sourceTransform = targetXForm;
            cs.weight = 1;

            //set up aimconstraint component on the freecontroller of containing atom
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>() == null) //make sure containing atom has a FCV3
            {
                Log.Message("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.", nameof(AimConstrain));
                return;
            }
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>() == null)
            {
                //there is no aimconstraint component yet, so create a new aimconstraint and add a new source
                AimConstraint ac = containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.AddComponent(typeof(AimConstraint)) as AimConstraint;
            }
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().sourceCount > 1)
            {
                //aimconstraint exists, but too many constraint sources, remove them all
                for(int i = 0; i < containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().sourceCount; i++)
                {
                    containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().RemoveSource(i);
                }
            }

            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().sourceCount == 1)
            {
                //use the existing constraint source
                containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().SetSource(0, cs);
            }
            else
            {
                //no constraint sources, so add a new one
                containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().AddSource(cs);
            }
            //set other aimconstraint parameters
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().aimVector.Set(0, 0, 1);
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().upVector.Set(0, 1, 0);
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().worldUpType = AimConstraint.WorldUpType.None;
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().weight = 1;
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().rotationAxis = Axis.X | Axis.Y; //change this to constrain axis
            containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().constraintActive = true;
            return;

        }
        void DisableAimConstraint() //removes all the constraint sources
        {
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>() == null) //make sure containing atom has a FCV3
            {
                Log.Message("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.", nameof(AimConstrain));
                return;
            }
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>() == null)
            {
                //there is no aimconstraint so do nothing
                return;
            }
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().sourceCount > 0)
            {
                //remove all constraint sources
                for(int i = 0; i < containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().sourceCount; i++)
                {
                    containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().RemoveSource(i);
                    containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>().gameObject.GetComponent<AimConstraint>().constraintActive = false;
                }
            }
        }
    }
}
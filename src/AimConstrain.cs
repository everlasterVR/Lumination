using System;
using UnityEngine;
using UnityEngine.Animations;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Text;
/****************************************************************************************
 * prestigitis_aimConstrain.cs
 * 
 * add this script to an atom to make the forward (Z) axis point to another atom. 
 * both atoms must have a FreeControllerV3
 * 
 * for continuous update when moving the containing atom, turn on physics for the containing atom
 * if containing atom is parented to another atom, will cause an exception
 * 
 * This work is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License
 * https://creativecommons.org/licenses/by-nc-sa/4.0/
 *
 ****************************************************************************************/

namespace prestigitis
{

    public class AimConstrain_20191104 : MVRScript
    {

        // IMPORTANT - DO NOT make custom enums. The dynamic C# complier crashes Unity when it encounters these for
        // some reason

        // IMPORTANT - DO NOT OVERRIDE Awake() as it is used internally by MVRScript - instead use Init() function which
        // is called right after creation

        protected ConstraintSource cs;
        protected Atom receivingAtom;
        protected JSONStorableStringChooser atomJSON;
        private UIDynamicTextField _infoTextField;

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

        protected void SetFCV3AsTarget(string receiverID)
        {
            //placeholder function
            return;
        }

        public override void Init()
        {
            try
            {
                // put init code in here
                if((bool) (SuperController.singleton))
                    SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnAtomRename);

                // create custom JSON storable params here if you want them to be stored with scene JSON
                // types are JSONStorableFloat, JSONStorableBool, JSONStorableString, JSONStorableStringChooser
                // JSONStorableColor

                var infoBuilder = new StringBuilder();

                infoBuilder.AppendLine("[prestigitis]");
                infoBuilder.AppendLine("aimConstrain 20191104");
                infoBuilder.AppendLine();
                infoBuilder.AppendLine("This script aims the forward (Z/blue) axis of its containing atom at the target atom.");
                infoBuilder.AppendLine();
                infoBuilder.AppendLine("Parent Atom must be 'None'.");
                infoBuilder.AppendLine();
                infoBuilder.AppendLine("Containing atom can be parent linked in position but not rotation.");
                infoBuilder.AppendLine();
                infoBuilder.AppendLine("For continuous update while moving the containing atom, turn on physics for the containing atom.");

                _infoTextField = CreateTextField(new JSONStorableString("info", infoBuilder.ToString()));
                _infoTextField.height = 600;
                _infoTextField.UItext.fontSize = 32;

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
                SuperController.LogError("Exception caught: " + e);
            }
        }

        // Start is called once before Update or FixedUpdate is called and after Init()
        void Start()
        {
            try
            {
                // put code in here
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
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
                SuperController.LogError("Exception caught: " + e);
            }
        }

        // Update is called with each rendered frame by Unity
        void Update()
        {
            try
            {
                // put code in here
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        // FixedUpdate is called with each physics simulation frame by Unity
        void FixedUpdate()
        {
            try
            {
                // put code in here
            }
            catch(Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
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
                SuperController.LogError("Exception caught: " + e);
            }
        }

        // OnDestroy is where you should put any cleanup
        // if you registered objects to supercontroller or atom, you should unregister them here
        void OnDestroy()
        {

        }

        void AddAimConstraintTargetingTransform(Transform targetXForm) //adds an aimconstraint to the containing atom, targeting the targetXForm
        {
            //set up constraint source using target transform
            cs.sourceTransform = targetXForm;
            cs.weight = 1;

            //set up aimconstraint component on the freecontroller of containing atom
            if(containingAtom.gameObject.GetComponentInChildren<FreeControllerV3>() == null) //make sure containing atom has a FCV3
            {
                SuperController.LogMessage("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.");
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
                SuperController.LogMessage("AimConstraint script needs to be on an atom with an active FreeControllerV3. Make sure atom is not parented.");
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
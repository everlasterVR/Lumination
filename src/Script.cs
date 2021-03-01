using SimpleJSON;
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
        private const string atomUidPrefix = "Illum_";
        private Dictionary<string, LightControl> lightControls = new Dictionary<string, LightControl>();

        private JSONStorableBool disableOtherLights;
        private List<Atom> disabledLights = new List<Atom>();

        private UIDynamicButton selectTargetButton;
        private JSONStorableBool dummyEnableAimAtTarget = new JSONStorableBool("Enable aiming at target", false);
        private UIDynamicToggle enableAimAtTargetToggle;
        private string selected = "";

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
                DisableOtherLightsUIToggle();

                UIDynamicButton addSpotLightButton = CreateButton("Add spot light");
                addSpotLightButton.button.onClick.AddListener(() => AddInvisibleLight("Spot"));

                UIDynamicButton addPointLightButton = CreateButton("Add point light");
                addPointLightButton.button.onClick.AddListener(() => AddInvisibleLight("Point"));

                UISpacer(25f);

                //right side

                UISpacer(100f, true);

                selectTargetButton = CreateButton("Select target to aim at", true);
                enableAimAtTargetToggle = CreateToggle(dummyEnableAimAtTarget, true);
                dummyEnableAimAtTarget.toggle.interactable = false;

                RefreshUI("");
                AddSuperControllerOnAtomActions();
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

        private void DisableOtherLightsUIToggle()
        {
            disableOtherLights = new JSONStorableBool("Disable other point and spot lights", false);
            UIDynamicToggle disableOtherLightsToggle = CreateToggle(disableOtherLights);
            disableOtherLights.toggle.onValueChanged.AddListener(val =>
            {
                if(val)
                {
                    DisableOtherPointAndSpotLights();
                }
                else
                {
                    EnableDisabledLights();
                }
            });
        }

        private void AddInvisibleLight(string lightType)
        {
            StartCoroutine(Tools.CreateAtomCo("InvisibleLight", $"{atomUidPrefix}InvisibleLight", (atom) =>
            {
                LightControl lc = gameObject.AddComponent<LightControl>();
                lc.Init(atom, lightType);
                string uid = atom.uid;
                lc.uiButton = UILightButton(uid);
                lightControls.Add(uid, lc);
                RefreshUI(uid);
            }));
        }

        private void UISpacer(float height, bool rightSide = false)
        {
            UIDynamic spacer = CreateSpacer(rightSide);
            spacer.height = height;
        }

        private UIDynamicButton UILightButton(string uid)
        {
            UIDynamicButton uiButton = CreateButton(uid);
            uiButton.button.onClick.AddListener(() => {
                RefreshUI(uid);
            });
            return uiButton;
        }

        private void RefreshUI(string uid)
        {
            if(lightControls.ContainsKey(selected))
            {
                RemoveToggle(lightControls[selected].enableLookAt);
            }
            else
            {
                RemoveToggle(dummyEnableAimAtTarget);
            }

            selected = uid;
            selectTargetButton.button.onClick.RemoveAllListeners();

            if(lightControls.ContainsKey(uid))
            {
                LightControl lc = lightControls[uid];
                CreateToggle(lc.enableLookAt, true);
                selectTargetButton.button.interactable = true;
                lc.enableLookAt.toggle.interactable = lc.target != null;

                selectTargetButton.button.onClick.AddListener(() =>
                {
                    lc.OnSelectTarget();
                    lc.enableLookAt.toggle.interactable = true;
                });
            }
            else
            {
                selectTargetButton.button.interactable = false;
                CreateToggle(dummyEnableAimAtTarget, true);
                dummyEnableAimAtTarget.toggle.interactable = false;
            }
        }

        public void OnEnable()
        {
            try
            {
                lightControls?.Values.ToList().ForEach(it => it.enabled = true);
                DisableOtherPointAndSpotLights();
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }

        private void DisableOtherPointAndSpotLights()
        {
            SuperController.singleton.GetAtoms().ForEach(atom => DisableAtomIfIsOtherLight(atom));
        }

        private bool DisableAtomIfIsOtherLight(Atom atom)
        {
            if(disableOtherLights == null || !disableOtherLights.val)
            {
                return false;
            }

            if(!atom.on || atom.type != "InvisibleLight" || atom.uid.StartsWith(atomUidPrefix) || lightControls.ContainsKey(atom.uid))
            {
                return false;
            }

            Light light = atom.GetComponentInChildren<Light>();
            if(light.type == LightType.Point || light.type == LightType.Spot)
            {
                atom.ToggleOn();
                disabledLights.Add(atom);
                return true;
            }

            return false;
        }

        private void EnableDisabledLights()
        {
            disabledLights?.ForEach(atom =>
            {
                if(!atom.on)
                {
                    atom.ToggleOn();
                }
            });
        }

        private void AddSuperControllerOnAtomActions()
        {
            SuperController.singleton.onAtomAddedHandlers += new SuperController.OnAtomAdded(OnAddAtom);
            SuperController.singleton.onAtomRemovedHandlers += new SuperController.OnAtomRemoved(OnRemoveAtom);
            SuperController.singleton.onAtomUIDRenameHandlers += new SuperController.OnAtomUIDRename(OnRenameAtom);
        }

        private void OnAddAtom(Atom atom)
        {
            bool wasDisabled = DisableAtomIfIsOtherLight(atom);
            if(wasDisabled)
            {
                Log.Message($"New InvisibleLight '{atom.uid}' was automatically disabled because 'Disable other point and spot lights' is checked in plugin UI.");
            }
        }

        private void OnRemoveAtom(Atom atom)
        {
            //light atom added by plugin was removed elsewhere
            if(lightControls.ContainsKey(atom.uid))
            {
                LightControl lc = lightControls[atom.uid];
                lightControls.Remove(atom.uid);
                RemoveButton(lc.uiButton);
                Destroy(lc);

                if(selected == atom.uid)
                {
                    RemoveToggle(lc.enableLookAt);
                }

                RefreshUI(lightControls?.Keys.FirstOrDefault() ?? "");
            }
            //other light atom disabled by plugin was removed
            if(disabledLights.Contains(atom))
            {
                disabledLights.Remove(atom);
            }
        }

        private void OnRenameAtom(string fromuid, string touid)
        {
            //light atom added by plugin was renamed elsewhere
            if(lightControls.ContainsKey(fromuid))
            {
                LightControl lc = lightControls[fromuid];
                lightControls.Remove(fromuid);
                lightControls.Add(touid, lc);
                lc.uiButton.label = touid;
                if(selected == fromuid)
                {
                    RefreshUI(touid);
                }
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);
            json["lightControls"] = new JSONArray();
            lightControls.Values.ToList().ForEach(lc => json["lightControls"].Add(lc.Serialize()));
            if(selected != "")
            {
                json["selected"] = selected;
            }
            json["disableOtherLights"].AsBool = disableOtherLights.val;
            needsStore = true;
            return json;
        }

        public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
            StartCoroutine(RestoreFromJSONInternal(json));
        }

        private IEnumerator RestoreFromJSONInternal(JSONClass json)
        {
            yield return new WaitForEndOfFrame();

            foreach(JSONClass lightJson in json["lightControls"].AsArray)
            {
                RestoreLightControlFromJSON(lightJson);
            }

            disableOtherLights.val = json["disableOtherLights"].AsBool;
            DisableOtherPointAndSpotLights();
            if(json["selected"] != null)
            {
                RefreshUI(json["selected"].Value);
            }
        }

        private void RestoreLightControlFromJSON(JSONClass lightJson)
        {
            string atomUid = lightJson["atomUid"].Value;
            Atom atom = SuperController.singleton.GetAtomByUid(atomUid);
            if(atom == null)
            {
                Log.Message($"Unable to control light atom '{atomUid}': " +
                    $"mentioned in saved JSON but not found in scene.");
            }
            else
            {
                LightControl lc = gameObject.AddComponent<LightControl>();

                FreeControllerV3 target = null;
                if(lightJson["aimingAtAtomUid"] != null && lightJson["aimingAtControl"] != null)
                {
                    string aimingAtAtomUid = lightJson["aimingAtAtomUid"].Value;
                    string aimingAtControl = lightJson["aimingAtControl"].Value;
                    Atom aimingAtAtom = SuperController.singleton.GetAtomByUid(aimingAtAtomUid);
                    target = aimingAtAtom?.gameObject
                        .GetComponentsInChildren<FreeControllerV3>()
                        .Where(it => it.name == aimingAtControl)
                        .FirstOrDefault();

                    if(target == null)
                    {
                        Log.Message($"Unable to point light atom '{atomUid}' at atom " +
                            $"'{aimingAtAtomUid}' target control '{aimingAtControl}': " +
                            $"target mentioned in saved JSON but not found in scene.");
                    }
                }

                lc.InitFromSave(atom, target, lightJson["enableLookAt"].AsBool);
                lc.uiButton = UILightButton(atom.uid);
                lightControls.Add(atom.uid, lc);
            }
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
                lightControls?.Values.ToList().ForEach(it => it.enabled = false);
                EnableDisabledLights();
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
                lightControls?.Values.ToList().ForEach(it => Destroy(it));
                SuperController.singleton.onAtomAddedHandlers -= new SuperController.OnAtomAdded(OnAddAtom);
                SuperController.singleton.onAtomRemovedHandlers -= new SuperController.OnAtomRemoved(OnRemoveAtom);
                SuperController.singleton.onAtomUIDRenameHandlers -= new SuperController.OnAtomUIDRename(OnRenameAtom);
            }
            catch(Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}

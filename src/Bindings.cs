using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lumination
{
    internal class Bindings : MonoBehaviour
    {
        private Lights lights;

        public Dictionary<string, string> Settings { get; set; }

        public List<object> OnKeyDownActions { get; set; }
        public JSONStorableAction OpenUIAction { get; set; }

        public void Init(Lights lights)
        {
            this.lights = lights;
            Settings = new Dictionary<string, string>
            {
                { "Namespace", nameof(Lumination) }
            };
            OnKeyDownActions = new List<object>()
            {
                OpenUI(),
            };
        }

        private object OpenUI()
        {
            OpenUIAction = new JSONStorableAction(nameof(OpenUI), () => lights.ShowUI(() => StartCoroutine(SelectPluginUICo())));
            return OpenUIAction;
        }

        //adapted from Timeline v4.3.1 (c) acidbubbles
        private IEnumerator SelectPluginUICo()
        {
            if(SuperController.singleton.gameMode != SuperController.GameMode.Edit)
            {
                SuperController.singleton.gameMode = SuperController.GameMode.Edit;
            }

            float time = 0f;
            while(time < 1f)
            {
                time += Time.unscaledDeltaTime;
                yield return null;

                var selector = lights.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                if(selector == null)
                {
                    continue;
                }

                selector.SetActiveTab("Plugins");
                if(lights.UITransform == null)
                {
                    Log.Error($"No UI", nameof(Bindings));
                }

                if(lights.enabled)
                {
                    lights.UITransform.gameObject.SetActive(true);
                }
                yield break;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lumination
{
    internal class Bindings : MonoBehaviour
    {
        private Log log = new Log(nameof(Bindings));
        private Lights lights;

        public Dictionary<string, string> Settings { get; set; }

        public List<object> OnKeyDownActions { get; set; }
        public JSONStorableAction OpenUIAction { get; set; }

        public void Init(Lights lights)
        {
            this.lights = lights;
            Settings = new Dictionary<string, string>
            {
                { "Namespace", Namespace() }
            };
            OnKeyDownActions = new List<object>()
            {
                OpenUI(),
            };
        }

        public void UpdateNamespace()
        {
            Settings["Namespace"] = Namespace();
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", lights, SendMessageOptions.DontRequireReceiver);
        }

        private object OpenUI()
        {
            OpenUIAction = new JSONStorableAction(nameof(OpenUI), () => lights.ShowUI(() => SelectPluginUI()));
            return OpenUIAction;
        }

        private void SelectPluginUI()
        {
            try
            {
                UITabSelector selector = lights.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
                selector.SetActiveTab("Plugins");
                if(lights.enabled)
                {
                    lights.UITransform.gameObject.SetActive(true);
                }
            }
            catch(Exception)
            {
                log.Error($"Unable to show plugin UI.");
            }
        }

        private string Namespace()
        {
            string uid = lights.containingAtom.uid;
            if(uid == Const.SUBSCENE_UID)
            {
                return nameof(Lumination);
            }

            return $"{nameof(Lumination)}:{uid}";
        }
    }
}

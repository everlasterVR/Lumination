using System.Collections.Generic;
using UnityEngine;

namespace Lumination
{
    internal class Bindings
    {
        private Lights lights;

        public Dictionary<string, string> Settings { get; set; }

        public List<object> OnKeyDownActions { get; set; }
        public JSONStorableAction OpenUIAction { get; set; }

        public Bindings(Lights lights)
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
            OpenUIAction = new JSONStorableAction(nameof(OpenUI), () =>
            {
                SuperController.singleton.SelectController(lights.GetMainController());
            });
            return OpenUIAction;
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

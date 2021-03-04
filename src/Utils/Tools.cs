using System;
using System.Collections;
using System.Collections.Generic;

namespace Illumination
{
    public static class Tools
    {
        public static Log log = new Log(nameof(Tools));

        //from Acidbubbles on Discord 27/02/2021
        public static IEnumerator CreateAtomCo(string type, string uid, Action<Atom> callback)
        {
            IEnumerator enumerator = SuperController.singleton.AddAtomByType(type, uid, true);
            while(enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
            Atom atom = SuperController.singleton.GetAtomByUid(uid);
            if(atom == null)
            {
                throw new NullReferenceException("Atom did not spawn");
            }
            callback(atom);
        }

        //adapted from Acidbubbles on Discord 27/02/2021
        public static string NewUID(string basename)
        {
            HashSet<string> uids = new HashSet<string>(SuperController.singleton.GetAtomUIDs());
            if(!uids.Contains(basename))
            {
                return basename;
            }

            for(int i = 2; i < 1000; i++)
            {
                string uid = basename + i;
                if(!uids.Contains(uid))
                {
                    return uid;
                }
            }

            return basename + Guid.NewGuid();
        }

        public static JSONStorableColor CopyColorStorable(JSONStorableColor source, bool callback)
        {
            JSONStorableColor copy = new JSONStorableColor(
                source.name,
                source.defaultVal
            );
            copy.val = source.val;

            if(callback)
            {
                copy.setJSONCallbackFunction = (jc) => source.val = jc.val;
                source.setJSONCallbackFunction = (jc) => copy.val = jc.val;
            }

            return copy;
        }

        public static JSONStorableFloat CopyFloatStorable(JSONStorableFloat source, bool callback)
        {
            JSONStorableFloat copy = new JSONStorableFloat(
                source.name,
                source.defaultVal,
                source.min,
                source.max,
                source.constrained
                //source.slider.interactable
            );
            copy.val = source.val;

            if(callback)
            {
                copy.setJSONCallbackFunction = (jc) => source.val = jc.val;
                source.setJSONCallbackFunction = (jc) => copy.val = jc.val;
            }

            return copy;
        }
    }
}

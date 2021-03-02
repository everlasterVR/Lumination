using System;
using System.Collections;
using System.Collections.Generic;

namespace Illumination
{
    public static class Tools
    {
        //from Acidbubbles on Discord 27/02/2021
        public static IEnumerator CreateAtomCo(string type, string name, Action<Atom> callback)
        {
            string uid = NewUid(name);
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

        //from Acidbubbles on Discord 27/02/2021
        private static string NewUid(string source)
        {
            var uids = new HashSet<string>(SuperController.singleton.GetAtomUIDs());
            for(int i = 1; i < 1000; i++)
            {
                string uid = source + i;
                if(!uids.Contains(uid))
                {
                    return uid;
                }
            }
            return source + Guid.NewGuid();
        }

        public static JSONStorableColor CopyColorStorable(JSONStorableColor source, bool callback)
        {
            JSONStorableColor copy = new JSONStorableColor(
                source.name,
                source.defaultVal
            );
            copy.val = source.val;

            //works only one way - change to source doesn't mirror back to the copy
            if(callback)
            {
                copy.setJSONCallbackFunction = (jc) => source.val = jc.val;
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

            //works only one way - change to source doesn't mirror back to the copy
            if(callback)
            {
                copy.setCallbackFunction = (val) => source.val = val;
            }

            return copy;
        }
    }
}

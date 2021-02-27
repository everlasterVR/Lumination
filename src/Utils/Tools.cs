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
    }
}

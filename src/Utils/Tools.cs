using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lumination
{
    public static class Tools
    {
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
                string uid = $"{basename}#{i}";
                if(!uids.Contains(uid))
                {
                    return uid;
                }
            }

            return basename + Guid.NewGuid();
        }

        public static JSONStorableColor CopyColorStorable(JSONStorableColor source, bool addCallbacks)
        {
            JSONStorableColor copy = new JSONStorableColor(
                source.name,
                source.defaultVal
            );
            copy.val = source.val;

            if(addCallbacks)
            {
                copy.setJSONCallbackFunction = (jc) => source.val = jc.val;
                source.setJSONCallbackFunction = (jc) => copy.val = jc.val;
            }

            return copy;
        }

        public static JSONStorableFloat CopyFloatStorable(JSONStorableFloat source, bool addCallbacks)
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

            if(addCallbacks)
            {
                copy.setJSONCallbackFunction = (jc) => source.val = jc.val;
                source.setJSONCallbackFunction = (jc) => copy.val = jc.val;
            }

            return copy;
        }

        public static void MoveToUnoccupiedPosition(Atom atom)
        {
            Vector3[] offsets = {
                new Vector3(-0.1f, 0, 0.0f),
                new Vector3(0.1f, 0, 0.0f),
                new Vector3(0.0f, 0, 0.1f),
                new Vector3(0.0f, 0, -0.1f)
            };
            Transform t = atom.GetComponentInChildren<Transform>();
            List<Vector3> atomPositions = SuperController.singleton.GetAtoms()
                .Where(sceneAtom => sceneAtom.uid != atom.uid)
                .Select(sceneAtom => sceneAtom.GetComponentInChildren<Transform>().position)
                .ToList();
            while(atomPositions.Any(pos => Vector3.Distance(pos, t.position) <= 0.1f))
            {
                t.Translate(offsets[new System.Random().Next(offsets.Length)], Space.World);
            }
        }

        //adapted from Atom.cs
        public static string WithoutSubScenePath(string uid)
        {
            return Regex.Replace(uid, SubscenePath(uid), "");
        }

        //adapted from Atom.cs
        private static string SubscenePath(string uid)
        {
            if(string.IsNullOrEmpty(uid))
            {
                return "";
            }

            string str = "";
            Atom atom = SuperController.singleton.GetAtomByUid(uid);
            if(atom == null)
            {
                return str;
            }

            for(Atom parent = atom.parentAtom; parent != null; parent = parent.parentAtom)
            {
                if(parent.isSubSceneType)
                {
                    str = parent.uidWithoutSubScenePath + "/" + str;
                }
            }
            return str;
        }
    }
}

using UnityEngine;

namespace Loki.Mods.Utility {
    
    public static class TransformExtensions {
        

        /// <summary>
        /// Outputs the hierarchy (up to a certain depth) of a transform to log.
        /// </summary>
        public static void DumpHierarchy(this Transform t, int depth)
        {
            Debug.Log(depth + ": " + t.name);
            foreach (Transform child in t)
            {
                DumpHierarchy(child, depth + 1);
            }
        }

        /// <summary>
        /// Outputs the component hierarchy of a transform to log.
        /// </summary>
        public static void DumpComponents(this Transform foundHead)
        {
            Debug.Log("Components on " + foundHead.name);
            foreach (var component in foundHead.GetComponents<Component>())
            {
                Debug.Log("Found component " + component.GetType().Name);
            }

            foreach (Transform t in foundHead)
            {
                DumpComponents(t);
            }
        }

        public static Transform FindTransform(this Transform root, params string[] path)
        {

            Transform output = root;
            for (int i = 0; i < path.Length; i++)
            {
                output = output.Find(path[i]);

                if (output == null) break;
            }

            return output;
        }
    }
    
}
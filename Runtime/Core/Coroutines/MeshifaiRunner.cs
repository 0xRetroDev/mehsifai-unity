using System.Collections;
using UnityEngine;

namespace MeshifAI.Core
{
    /// <summary>
    /// Runner that handles coroutines for the Meshifai API
    /// This allows using the API without directly calling StartCoroutine
    /// </summary>
    internal class MeshifaiRunner : MonoBehaviour
    {
        private static MeshifaiRunner _instance;

        /// <summary>
        /// Gets the singleton instance of MeshifaiRunner
        /// </summary>
        private static MeshifaiRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a new GameObject with the runner
                    GameObject go = new GameObject("MeshifaiRunner");
                    _instance = go.AddComponent<MeshifaiRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Helper method to run a coroutine from a static context
        /// </summary>
        internal static Coroutine RunCoroutine(IEnumerator coroutine)
        {
            return Instance.StartCoroutine(coroutine);
        }

        /// <summary>
        /// Helper method to stop a coroutine from a static context
        /// </summary>
        internal static void StopRunner(Coroutine coroutine)
        {
            if (coroutine != null && _instance != null)
            {
                Instance.StopCoroutine(coroutine);
            }
        }
    }
}
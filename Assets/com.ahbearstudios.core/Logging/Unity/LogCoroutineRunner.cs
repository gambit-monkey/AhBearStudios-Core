using UnityEngine;
using AhBearStudios.Core.Logging.Interfaces;

namespace AhBearStudios.Core.Logging.Unity
{
    /// <summary>
    /// Unity MonoBehaviour-based coroutine runner for the logging system.
    /// </summary>
    public class LogCoroutineRunner : MonoBehaviour, ILogCoroutineRunner
    {
        private static LogCoroutineRunner _instance;
        
        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        public static LogCoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[LogCoroutineRunner]");
                    _instance = go.AddComponent<LogCoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
using UnityEngine;
using UnityEditor;
using AhBearStudios.Core.Profiling.Unity;

namespace AhBearStudios.Core.Profiling.Editor
{
    /// <summary>
    /// Editor utilities for creating and managing profiler UI components.
    /// </summary>
    public static class ProfilerUICreationUtility
    {
        /// <summary>
        /// Creates a new RuntimeProfilerUI instance in the scene.
        /// </summary>
        [MenuItem("Tools/AhBear Studios/Profiling/Create Runtime UI")]
        public static void CreateRuntimeUI()
        {
            var existing = Object.FindFirstObjectByType<RuntimeProfilerUI>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }
            
            // Find or create a ProfileManager
            var profileManager = ProfileManager.Instance;
            
            // Create the UI
            var factory = new RuntimeProfilerUIFactory(profileManager)
                .SetShowMetrics(true)
                .SetShowSessions(true)
                .SetMaxVisibleItems(20)
                .SetUpdateInterval(0.5f);
                
            var profilerUI = factory.Create();
            
            // Select the created UI in the editor
            Selection.activeGameObject = profilerUI.gameObject;
            EditorGUIUtility.PingObject(profilerUI.gameObject);
            
            Debug.Log("RuntimeProfilerUI created successfully");
        }
    }
}
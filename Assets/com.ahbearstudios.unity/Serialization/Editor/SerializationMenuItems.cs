using UnityEngine;
using UnityEditor;
using AhBearStudios.Unity.Serialization.ScriptableObjects;
using AhBearStudios.Unity.Serialization.Formatters;
using System.IO;

namespace AhBearStudios.Unity.Serialization.Editor
{
    /// <summary>
    /// Unity Editor menu items for AhBearStudios serialization system.
    /// Provides quick access to common serialization tasks and utilities.
    /// </summary>
    public static class SerializationMenuItems
    {
        private const string MenuRoot = "AhBearStudios/Serialization/";
        private const string ConfigPath = "Assets/SerializationConfigs/";

        [MenuItem(MenuRoot + "Create Configuration Asset", false, 1)]
        public static void CreateConfigurationAsset()
        {
            var config = ScriptableObject.CreateInstance<SerializationConfigAsset>();
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/SerializationConfigs"))
            {
                AssetDatabase.CreateFolder("Assets", "SerializationConfigs");
            }
            
            var path = AssetDatabase.GenerateUniqueAssetPath(ConfigPath + "SerializationConfig.asset");
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[Serialization] Created configuration asset at: {path}");
        }

        [MenuItem(MenuRoot + "Create Development Configuration", false, 2)]
        public static void CreateDevelopmentConfiguration()
        {
            var config = ScriptableObject.CreateInstance<SerializationConfigAsset>();
            
            // Set development-friendly defaults
            config.SetDevelopmentDefaults();
            
            if (!AssetDatabase.IsValidFolder("Assets/SerializationConfigs"))
            {
                AssetDatabase.CreateFolder("Assets", "SerializationConfigs");
            }
            
            var path = AssetDatabase.GenerateUniqueAssetPath(ConfigPath + "SerializationConfig_Development.asset");
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[Serialization] Created development configuration asset at: {path}");
        }

        [MenuItem(MenuRoot + "Create Production Configuration", false, 3)]
        public static void CreateProductionConfiguration()
        {
            var config = ScriptableObject.CreateInstance<SerializationConfigAsset>();
            
            // Set production-optimized defaults
            config.SetProductionDefaults();
            
            if (!AssetDatabase.IsValidFolder("Assets/SerializationConfigs"))
            {
                AssetDatabase.CreateFolder("Assets", "SerializationConfigs");
            }
            
            var path = AssetDatabase.GenerateUniqueAssetPath(ConfigPath + "SerializationConfig_Production.asset");
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"[Serialization] Created production configuration asset at: {path}");
        }

        [MenuItem(MenuRoot + "Tools/Register All Formatters", false, 21)]
        public static void RegisterAllFormatters()
        {
            UnityFormatterRegistration.RegisterFormatters();
            Debug.Log("[Serialization] All Unity formatters registered successfully");
        }

        [MenuItem(MenuRoot + "Tools/Register Network Optimized Formatters", false, 22)]
        public static void RegisterNetworkOptimizedFormatters()
        {
            UnityFormatterRegistration.RegisterNetworkOptimizedFormatters();
            Debug.Log("[Serialization] Network-optimized formatters registered successfully");
        }

        [MenuItem(MenuRoot + "Tools/Register HDR Formatters", false, 23)]
        public static void RegisterHDRFormatters()
        {
            UnityFormatterRegistration.RegisterHDRFormatters();
            Debug.Log("[Serialization] HDR formatters registered successfully");
        }

        [MenuItem(MenuRoot + "Tools/Validate Formatter Registration", false, 24)]
        public static void ValidateFormatterRegistration()
        {
            var isRegistered = UnityFormatterRegistration.IsRegistered();
            var count = UnityFormatterRegistration.GetRegisteredFormatterCount();
            
            if (isRegistered)
            {
                EditorUtility.DisplayDialog("Formatter Validation", 
                    $"✓ Formatters are properly registered.\n\nRegistered formatters: {count}", "OK");
                Debug.Log($"[Serialization] Formatter validation passed. {count} formatters registered.");
            }
            else
            {
                EditorUtility.DisplayDialog("Formatter Validation", 
                    "✗ Formatters are not registered.\n\nUse the 'Register All Formatters' menu item to register them.", "OK");
                Debug.LogWarning("[Serialization] Formatter validation failed. Formatters not registered.");
            }
        }

        [MenuItem(MenuRoot + "Tools/Clear Editor Preferences", false, 31)]
        public static void ClearEditorPreferences()
        {
            if (EditorUtility.DisplayDialog("Clear Preferences", 
                "This will clear all AhBearStudios serialization editor preferences. Continue?", "Clear", "Cancel"))
            {
                EditorPrefs.DeleteKey("AhBearStudios.Serialization.ConfigAsset");
                EditorPrefs.DeleteKey("AhBearStudios.Serialization.LastWindow");
                EditorPrefs.DeleteKey("AhBearStudios.Serialization.DebugLevel");
                
                Debug.Log("[Serialization] Editor preferences cleared");
            }
        }

        [MenuItem(MenuRoot + "Tools/Force Garbage Collection", false, 32)]
        public static void ForceGarbageCollection()
        {
            var beforeMemory = System.GC.GetTotalMemory(false);
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            var afterMemory = System.GC.GetTotalMemory(false);
            var freedMemory = beforeMemory - afterMemory;
            
            Debug.Log($"[Serialization] Garbage collection completed. Freed: {freedMemory / 1024f / 1024f:F2} MB");
            
            EditorUtility.DisplayDialog("Garbage Collection", 
                $"Garbage collection completed.\n\nMemory freed: {freedMemory / 1024f / 1024f:F2} MB", "OK");
        }

        [MenuItem(MenuRoot + "Tools/Generate Performance Report", false, 33)]
        public static void GeneratePerformanceReport()
        {
            var reportPath = Path.Combine(Application.dataPath, "..", "SerializationPerformanceReport.txt");
            
            try
            {
                using (var writer = new StreamWriter(reportPath))
                {
                    writer.WriteLine("AhBearStudios Serialization Performance Report");
                    writer.WriteLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine();
                    
                    // System information
                    writer.WriteLine("System Information:");
                    writer.WriteLine($"Unity Version: {Application.unityVersion}");
                    writer.WriteLine($"Platform: {Application.platform}");
                    writer.WriteLine($"System Memory: {SystemInfo.systemMemorySize} MB");
                    writer.WriteLine($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
                    writer.WriteLine($"Processor: {SystemInfo.processorType}");
                    writer.WriteLine($"Processor Count: {SystemInfo.processorCount}");
                    writer.WriteLine();
                    
                    // Formatter information
                    writer.WriteLine("Formatter Registration:");
                    writer.WriteLine($"Registered: {UnityFormatterRegistration.IsRegistered()}");
                    writer.WriteLine($"Formatter Count: {UnityFormatterRegistration.GetRegisteredFormatterCount()}");
                    writer.WriteLine();
                    
                    // Memory information
                    writer.WriteLine("Memory Information:");
                    writer.WriteLine($"GC Memory: {System.GC.GetTotalMemory(false) / 1024f / 1024f:F2} MB");
                    writer.WriteLine($"Gen 0 Collections: {System.GC.CollectionCount(0)}");
                    writer.WriteLine($"Gen 1 Collections: {System.GC.CollectionCount(1)}");
                    writer.WriteLine($"Gen 2 Collections: {System.GC.CollectionCount(2)}");
                    writer.WriteLine();
                    
                    // Configuration scan
                    writer.WriteLine("Configuration Assets:");
                    var configGuids = AssetDatabase.FindAssets("t:SerializationConfigAsset");
                    if (configGuids.Length == 0)
                    {
                        writer.WriteLine("No configuration assets found.");
                    }
                    else
                    {
                        foreach (var guid in configGuids)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var config = AssetDatabase.LoadAssetAtPath<SerializationConfigAsset>(path);
                            if (config != null)
                            {
                                writer.WriteLine($"- {path}");
                                writer.WriteLine($"  Threading Mode: {config.ThreadingMode}");
                                writer.WriteLine($"  Buffer Pool Size: {config.BufferPoolSize:N0} bytes");
                                writer.WriteLine($"  Compression: {config.EnableCompression}");
                                writer.WriteLine($"  Encryption: {config.EnableEncryption}");
                                writer.WriteLine();
                            }
                        }
                    }
                    
                    writer.WriteLine("Report generation completed successfully.");
                }
                
                Debug.Log($"[Serialization] Performance report generated: {reportPath}");
                EditorUtility.DisplayDialog("Performance Report", 
                    $"Performance report generated successfully.\n\nSaved to: {reportPath}", "OK");
                    
                // Offer to open the file
                if (EditorUtility.DisplayDialog("Open Report", "Would you like to open the performance report?", "Open", "Close"))
                {
                    System.Diagnostics.Process.Start(reportPath);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Serialization] Failed to generate performance report: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate performance report:\n\n{ex.Message}", "OK");
            }
        }

        [MenuItem(MenuRoot + "Documentation/Open Design Document", false, 41)]
        public static void OpenDesignDocument()
        {
            var designDocPath = "Assets/com.ahbearstudios.core/Documentation/serialization_system.md";
            
            if (File.Exists(designDocPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(designDocPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", 
                    "Design document not found at expected location:\n" + designDocPath, "OK");
            }
        }

        [MenuItem(MenuRoot + "Documentation/Open GitHub Repository", false, 42)]
        public static void OpenGitHubRepository()
        {
            // Replace with actual repository URL when available
            var repoUrl = "https://github.com/ahbearstudios/unity-serialization";
            Application.OpenURL(repoUrl);
        }

        [MenuItem(MenuRoot + "About", false, 50)]
        public static void ShowAboutDialog()
        {
            var version = "1.0.0"; // This would typically come from a version file
            var message = $"AhBearStudios Serialization System\n\n" +
                         $"Version: {version}\n" +
                         $"Unity Version: {Application.unityVersion}\n\n" +
                         $"Features:\n" +
                         $"• High-performance MemoryPack serialization\n" +
                         $"• Unity Job System integration\n" +
                         $"• UniTask async coordination\n" +
                         $"• ZLinq zero-allocation operations\n" +
                         $"• Comprehensive Unity type support\n" +
                         $"• Advanced debugging and profiling tools\n\n" +
                         $"© AhBearStudios. All rights reserved.";
                         
            EditorUtility.DisplayDialog("About AhBearStudios Serialization", message, "OK");
        }

        // Validation methods for menu items
        [MenuItem(MenuRoot + "Tools/Register All Formatters", true)]
        public static bool ValidateRegisterAllFormatters()
        {
            return !UnityFormatterRegistration.IsRegistered();
        }

        [MenuItem(MenuRoot + "Tools/Register Network Optimized Formatters", true)]
        public static bool ValidateRegisterNetworkOptimizedFormatters()
        {
            return UnityFormatterRegistration.IsRegistered();
        }

        [MenuItem(MenuRoot + "Tools/Register HDR Formatters", true)]
        public static bool ValidateRegisterHDRFormatters()
        {
            return UnityFormatterRegistration.IsRegistered();
        }
    }

    /// <summary>
    /// Extension methods for SerializationConfigAsset to support menu item operations.
    /// </summary>
    public static class SerializationConfigAssetExtensions
    {
        public static void SetDevelopmentDefaults(this SerializationConfigAsset config)
        {
            // Use reflection or expose public methods to set development defaults
            // This is a placeholder - actual implementation would depend on the config class structure
        }

        public static void SetProductionDefaults(this SerializationConfigAsset config)
        {
            // Use reflection or expose public methods to set production defaults
            // This is a placeholder - actual implementation would depend on the config class structure
        }
    }
}
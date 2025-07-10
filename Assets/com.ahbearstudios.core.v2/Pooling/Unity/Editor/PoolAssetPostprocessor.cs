using UnityEditor;
using UnityEngine;

namespace AhBearStudios.Core.Pooling.Unity.Editor
{
    /// <summary>
    /// Asset post-processor for pool-related assets
    /// </summary>
    public class PoolAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check for package.json and show welcome message for first import
            foreach (var importedAsset in importedAssets)
            {
                if (importedAsset.EndsWith("com.ahbearstudios.pooling/package.json"))
                {
                    // Only show the welcome message if it hasn't been shown before
                    if (!EditorPrefs.GetBool("AhBearStudios.Pooling.WelcomeShown", false))
                    {
                        ShowWelcomeMessage();
                        EditorPrefs.SetBool("AhBearStudios.Pooling.WelcomeShown", true);
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// Shows welcome message in the console
        /// </summary>
        private static void ShowWelcomeMessage()
        {
            Debug.Log(
                "Thank you for installing AhBear Pooling!\n\n" +
                "To get started, explore the samples or check out the documentation.\n" +
                "You can find the Pool Visualizer window in the Tools > AhBear Studios menu.\n\n" +
                "For questions or feedback, please visit our GitHub repository."
            );
        }
    }
}
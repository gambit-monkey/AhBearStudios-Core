#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using AhBearStudios.Core.Profiling.Unity;

namespace AhBearStudios.Core.Profiling.Editor
{
    /// <summary>
    /// Setup wizard for quickly configuring the profiling system in a project.
    /// Provides a guided setup process for profiling components and configuration.
    /// </summary>
    public class ProfilingSetupWizard : EditorWindow
    {
        private enum SetupStep
        {
            Welcome = 0,
            CoreComponents = 1,
            Configuration = 2,
            RuntimeUI = 3,
            SceneIntegration = 4,
            Complete = 5
        }
        
        private SetupStep _currentStep = SetupStep.Welcome;
        private Vector2 _scrollPosition;
        
        // Setup options
        private bool _createProfileManager = true;
        private bool _createDependencyProvider = true;
        private bool _createMessageBusProvider = true;
        private bool _createConfiguration = true;
        private bool _createRuntimeUI = true;
        private bool _createSceneManager = true;
        private bool _setupDefaultAlerts = true;
        private bool _enableAttributeProfiling = true;
        
        // UI options
        private bool _createCanvas = true;
        private bool _useScreenSpaceOverlay = true;
        private KeyCode _toggleKey = KeyCode.F3;
        
        // Configuration options
        private string _configurationName = "DefaultProfilerConfiguration";
        private string _configurationPath = "Assets/Settings/Profiling";
        
        // Runtime UI prefab path
        private string _runtimeUIPrefabPath = "Assets/Prefabs/Profiling";
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _stepStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;
        
        [MenuItem("Tools/AhBear Studios/Profiling/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProfilingSetupWizard>("Profiling Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.maxSize = new Vector2(800, 800);
            window.Show();
        }
        
        private void OnEnable()
        {
            _currentStep = SetupStep.Welcome;
            
            // Initialize default paths
            if (!Directory.Exists(_configurationPath))
            {
                _configurationPath = "Assets";
            }
            
            if (!Directory.Exists(_runtimeUIPrefabPath))
            {
                _runtimeUIPrefabPath = "Assets";
            }
        }
        
        private void OnGUI()
        {
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }
            
            DrawHeader();
            DrawProgressBar();
            
            EditorGUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentStep)
            {
                case SetupStep.Welcome:
                    DrawWelcomeStep();
                    break;
                case SetupStep.CoreComponents:
                    DrawCoreComponentsStep();
                    break;
                case SetupStep.Configuration:
                    DrawConfigurationStep();
                    break;
                case SetupStep.RuntimeUI:
                    DrawRuntimeUIStep();
                    break;
                case SetupStep.SceneIntegration:
                    DrawSceneIntegrationStep();
                    break;
                case SetupStep.Complete:
                    DrawCompleteStep();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawNavigationButtons();
        }
        
        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
            
            _stepStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue }
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 30,
                fontSize = 12
            };
            
            _stylesInitialized = true;
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Profiling System Setup Wizard", _headerStyle);
            EditorGUILayout.Space(10);
        }
        
        private void DrawProgressBar()
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            var progress = (float)_currentStep / (float)(Enum.GetValues(typeof(SetupStep)).Length - 1);
            EditorGUI.ProgressBar(rect, progress, $"Step {(int)_currentStep + 1} of {Enum.GetValues(typeof(SetupStep)).Length}");
        }
        
        private void DrawWelcomeStep()
        {
            EditorGUILayout.LabelField("Welcome to the Profiling Setup Wizard", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("This wizard will help you set up the AhBear Studios Profiling System in your project.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("The profiling system provides:", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("• Real-time performance monitoring", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Unity lifecycle profiling", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Custom code profiling with attributes", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Runtime visualization UI", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Threshold-based alerting", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Scene and GameObject profiling", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Click 'Next' to begin the setup process.", EditorStyles.wordWrappedLabel);
        }
        
        private void DrawCoreComponentsStep()
        {
            EditorGUILayout.LabelField("Core Components Setup", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Select which core components to create:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _createProfileManager = EditorGUILayout.ToggleLeft("Profile Manager (Required)", _createProfileManager);
            EditorGUILayout.LabelField("    Central manager for all profiling operations", EditorStyles.miniLabel);
            
            _createDependencyProvider = EditorGUILayout.ToggleLeft("Dependency Provider", _createDependencyProvider);
            EditorGUILayout.LabelField("    Provides dependency injection services", EditorStyles.miniLabel);
            
            _createMessageBusProvider = EditorGUILayout.ToggleLeft("Message Bus Provider", _createMessageBusProvider);
            EditorGUILayout.LabelField("    Handles inter-component messaging", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            if (!_createProfileManager)
            {
                EditorGUILayout.HelpBox("Profile Manager is required for the profiling system to function.", MessageType.Warning);
            }
        }
        
        private void DrawConfigurationStep()
        {
            EditorGUILayout.LabelField("Configuration Setup", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Profiler Configuration:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _createConfiguration = EditorGUILayout.ToggleLeft("Create Configuration Asset", _createConfiguration);
            
            if (_createConfiguration)
            {
                EditorGUILayout.Space(5);
                _configurationName = EditorGUILayout.TextField("Configuration Name", _configurationName);
                
                EditorGUILayout.BeginHorizontal();
                _configurationPath = EditorGUILayout.TextField("Save Path", _configurationPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Configuration Path", _configurationPath, "");
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    {
                        _configurationPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                _setupDefaultAlerts = EditorGUILayout.ToggleLeft("Setup Default Threshold Alerts", _setupDefaultAlerts);
                _enableAttributeProfiling = EditorGUILayout.ToggleLeft("Enable Attribute-Based Profiling", _enableAttributeProfiling);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuntimeUIStep()
        {
            EditorGUILayout.LabelField("Runtime UI Setup", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Runtime Visualization UI:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _createRuntimeUI = EditorGUILayout.ToggleLeft("Create Runtime UI", _createRuntimeUI);
            
            if (_createRuntimeUI)
            {
                EditorGUILayout.Space(5);
                _createCanvas = EditorGUILayout.ToggleLeft("Create UI Canvas", _createCanvas);
                
                if (_createCanvas)
                {
                    _useScreenSpaceOverlay = EditorGUILayout.ToggleLeft("Use Screen Space Overlay", _useScreenSpaceOverlay);
                }
                
                EditorGUILayout.Space(5);
                _toggleKey = (KeyCode)EditorGUILayout.EnumPopup("Toggle Key", _toggleKey);
                
                EditorGUILayout.BeginHorizontal();
                _runtimeUIPrefabPath = EditorGUILayout.TextField("Prefab Save Path", _runtimeUIPrefabPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Prefab Path", _runtimeUIPrefabPath, "");
                    if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    {
                        _runtimeUIPrefabPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSceneIntegrationStep()
        {
            EditorGUILayout.LabelField("Scene Integration Setup", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Scene Lifecycle Profiling:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _createSceneManager = EditorGUILayout.ToggleLeft("Create Scene Manager", _createSceneManager);
            EditorGUILayout.LabelField("    Automatically profiles scene loading and GameObject lifecycle", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Additional Options:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (GUILayout.Button("Add Profiling to Selected GameObjects"))
            {
                AddProfilingToSelected();
            }
            
            if (GUILayout.Button("Create Example Profiled Scripts"))
            {
                CreateExampleScripts();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCompleteStep()
        {
            EditorGUILayout.LabelField("Setup Complete!", _stepStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("The profiling system has been set up successfully!", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("What's been created:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (_createProfileManager)
                EditorGUILayout.LabelField("✓ Profile Manager", EditorStyles.wordWrappedLabel);
            if (_createDependencyProvider)
                EditorGUILayout.LabelField("✓ Dependency Provider", EditorStyles.wordWrappedLabel);
            if (_createMessageBusProvider)
                EditorGUILayout.LabelField("✓ Message Bus Provider", EditorStyles.wordWrappedLabel);
            if (_createConfiguration)
                EditorGUILayout.LabelField("✓ Profiler Configuration", EditorStyles.wordWrappedLabel);
            if (_createRuntimeUI)
                EditorGUILayout.LabelField("✓ Runtime UI", EditorStyles.wordWrappedLabel);
            if (_createSceneManager)
                EditorGUILayout.LabelField("✓ Scene Manager", EditorStyles.wordWrappedLabel);
                
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Next steps:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("• Enter Play Mode to start profiling", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"• Press {_toggleKey} to toggle the runtime UI", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Add [ProfileMethod] attributes to your methods", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("• Use ProfiledGameObject component for automatic profiling", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Open Documentation", _buttonStyle))
            {
                Application.OpenURL("https://docs.ahbearstudios.com/profiling");
            }
        }
        
        private void DrawNavigationButtons()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = _currentStep > SetupStep.Welcome;
            if (GUILayout.Button("Previous", _buttonStyle))
            {
                _currentStep--;
            }
            
            GUILayout.FlexibleSpace();
            
            GUI.enabled = true;
            if (_currentStep < SetupStep.Complete)
            {
                if (GUILayout.Button("Next", _buttonStyle))
                {
                    if (ValidateCurrentStep())
                    {
                        if (_currentStep == SetupStep.SceneIntegration)
                        {
                            PerformSetup();
                        }
                        _currentStep++;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Finish", _buttonStyle))
                {
                    Close();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUI.enabled = true;
        }
        
        private bool ValidateCurrentStep()
        {
            switch (_currentStep)
            {
                case SetupStep.CoreComponents:
                    if (!_createProfileManager)
                    {
                        EditorUtility.DisplayDialog("Validation Error", "Profile Manager is required for the profiling system to function.", "OK");
                        return false;
                    }
                    break;
                    
                case SetupStep.Configuration:
                    if (_createConfiguration && string.IsNullOrEmpty(_configurationName))
                    {
                        EditorUtility.DisplayDialog("Validation Error", "Configuration name cannot be empty.", "OK");
                        return false;
                    }
                    break;
            }
            
            return true;
        }
        
        private void PerformSetup()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Setting up Profiling System", "Creating components...", 0.1f);
                
                // Create core components
                CreateCoreComponents();
                
                EditorUtility.DisplayProgressBar("Setting up Profiling System", "Creating configuration...", 0.4f);
                
                // Create configuration
                if (_createConfiguration)
                {
                    CreateConfigurationAsset();
                }
                
                EditorUtility.DisplayProgressBar("Setting up Profiling System", "Setting up UI...", 0.7f);
                
                // Create runtime UI
                if (_createRuntimeUI)
                {
                    CreateRuntimeUIComponents();
                }
                
                EditorUtility.DisplayProgressBar("Setting up Profiling System", "Finalizing setup...", 0.9f);
                
                // Final setup steps
                FinalizeSetup();
                
                EditorUtility.DisplayProgressBar("Setting up Profiling System", "Complete!", 1.0f);
                
                EditorUtility.ClearProgressBar();
                
                Debug.Log("[ProfilingSetupWizard] Setup completed successfully!");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Setup Error", $"An error occurred during setup: {ex.Message}", "OK");
                Debug.LogError($"[ProfilingSetupWizard] Setup failed: {ex}");
            }
        }
        
        private void CreateCoreComponents()
        {
            if (_createProfileManager)
            {
                var existing = FindObjectOfType<ProfileManager>();
                if (existing == null)
                {
                    var go = new GameObject("Profile Manager");
                    go.AddComponent<ProfileManager>();
                    Selection.activeGameObject = go;
                }
            }
            
            if (_createDependencyProvider)
            {
                var existing = FindObjectOfType<DependencyProvider>();
                if (existing == null)
                {
                    var go = new GameObject("Dependency Provider");
                    go.AddComponent<DependencyProvider>();
                }
            }
            
            if (_createMessageBusProvider)
            {
                var existing = FindObjectOfType<MessageBusProvider>();
                if (existing == null)
                {
                    var go = new GameObject("Message Bus Provider");
                    go.AddComponent<MessageBusProvider>();
                }
            }
            
            if (_createSceneManager)
            {
                var existing = FindObjectOfType<ProfilingSceneManager>();
                if (existing == null)
                {
                    var go = new GameObject("Profiling Scene Manager");
                    go.AddComponent<ProfilingSceneManager>();
                }
            }
        }
        
        private void CreateConfigurationAsset()
        {
            if (!Directory.Exists(_configurationPath))
            {
                Directory.CreateDirectory(_configurationPath);
            }
            
            var config = ScriptableObject.CreateInstance<ProfilerConfiguration>();
            config.InitializeDefaults();
            
            string assetPath = Path.Combine(_configurationPath, $"{_configurationName}.asset");
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            
            // Assign to ProfileManager if it exists
            var profileManager = FindObjectOfType<ProfileManager>();
            if (profileManager != null)
            {
                var serializedObject = new SerializedObject(profileManager);
                var configProperty = serializedObject.FindProperty("_configuration");
                configProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        private void CreateRuntimeUIComponents()
        {
            var existing = FindObjectOfType<RuntimeProfilerUI>();
            if (existing != null)
                return;
                
            GameObject uiRoot = null;
            
            if (_createCanvas)
            {
                uiRoot = new GameObject("Runtime Profiler UI");
                var canvas = uiRoot.AddComponent<Canvas>();
                canvas.renderMode = _useScreenSpaceOverlay ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
                canvas.sortingOrder = 1000;
                
                uiRoot.AddComponent<CanvasScaler>();
                uiRoot.AddComponent<GraphicRaycaster>();
            }
            else
            {
                uiRoot = new GameObject("Runtime Profiler UI");
            }
            
            var runtimeUI = uiRoot.AddComponent<RuntimeProfilerUI>();
            
            // Create basic UI structure here if needed
            CreateBasicUIElements(uiRoot);
            
            // Save as prefab if path is specified
            if (!string.IsNullOrEmpty(_runtimeUIPrefabPath))
            {
                if (!Directory.Exists(_runtimeUIPrefabPath))
                {
                    Directory.CreateDirectory(_runtimeUIPrefabPath);
                }
                
                string prefabPath = Path.Combine(_runtimeUIPrefabPath, "RuntimeProfilerUI.prefab");
                PrefabUtility.SaveAsPrefabAsset(uiRoot, prefabPath);
            }
        }
        
        private void CreateBasicUIElements(GameObject parent)
        {
            // Create a simple panel for the UI
            var panel = new GameObject("Panel");
            panel.transform.SetParent(parent.transform, false);
            
            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.7f);
            rectTransform.anchorMax = new Vector2(0.4f, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            
            // Create header text
            var headerText = new GameObject("Header Text");
            headerText.transform.SetParent(panel.transform, false);
            
            var headerRect = headerText.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.8f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = new Vector2(10, 0);
            headerRect.offsetMax = new Vector2(-10, 0);
            
            var headerTextComponent = headerText.AddComponent<Text>();
            headerTextComponent.text = "PROFILER";
            headerTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            headerTextComponent.fontSize = 16;
            headerTextComponent.color = Color.white;
            headerTextComponent.alignment = TextAnchor.MiddleCenter;
            
            // Create scroll view for metrics
            var scrollView = new GameObject("Scroll View");
            scrollView.transform.SetParent(panel.transform, false);
            
            var scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 0.8f);
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -5);
            
            var scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            
            // Create content area
            var content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            scrollComponent.content = contentRect;
        }
        
        private void FinalizeSetup()
        {
            // Ensure all assets are saved
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Set up default scenes if needed
            if (_createSceneManager)
            {
                AddSceneManagerToCurrentScene();
            }
            
            // Configure attribute profiling if enabled
            if (_enableAttributeProfiling)
            {
                SetupAttributeProfiling();
            }
        }
        
        private void AddSceneManagerToCurrentScene()
        {
            var existing = FindObjectOfType<ProfilingSceneManager>();
            if (existing == null)
            {
                var go = new GameObject("Profiling Scene Manager");
                go.AddComponent<ProfilingSceneManager>();
            }
        }
        
        private void SetupAttributeProfiling()
        {
            var existing = FindObjectOfType<AttributeProfilerBehaviour>();
            if (existing == null)
            {
                var go = new GameObject("Attribute Profiler");
                go.AddComponent<AttributeProfilerBehaviour>();
            }
        }
        
        private void AddProfilingToSelected()
        {
            var selected = Selection.gameObjects;
            int added = 0;
            
            foreach (var go in selected)
            {
                if (go.GetComponent<ProfiledGameObject>() == null)
                {
                    go.AddComponent<ProfiledGameObject>();
                    added++;
                }
            }
            
            if (added > 0)
            {
                EditorUtility.DisplayDialog("Profiling Added", $"Added profiling to {added} GameObjects.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Changes", "No GameObjects were selected or they already have profiling.", "OK");
            }
        }
        
        private void CreateExampleScripts()
        {
            string examplePath = "Assets/Examples/Profiling";
            
            if (!Directory.Exists(examplePath))
            {
                Directory.CreateDirectory(examplePath);
            }
            
            CreateExampleProfiledScript(examplePath);
            CreateExampleManagerScript(examplePath);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Examples Created", 
                $"Example scripts have been created in {examplePath}", "OK");
        }
        
        private void CreateExampleProfiledScript(string path)
        {
            string scriptContent = @"using UnityEngine;
using AhBearStudios.Core.Profiling.Attributes;
using AhBearStudios.Core.Profiling.Unity;

namespace Examples.Profiling
{
    /// <summary>
    /// Example script showing how to use the profiling system with attributes
    /// </summary>
    [ProfileClass(""Gameplay"", ""Example"")]
    public class ExampleProfiledBehaviour : MonoBehaviour
    {
        [Header(""Profiling Settings"")]
        [SerializeField] private bool _enableManualProfiling = true;
        
        private ProfiledGameObject _profiledGameObject;
        
        private void Awake()
        {
            // Get the ProfiledGameObject component
            _profiledGameObject = GetComponent<ProfiledGameObject>();
            if (_profiledGameObject == null)
            {
                _profiledGameObject = gameObject.AddComponent<ProfiledGameObject>();
            }
        }
        
        // This method will be automatically profiled due to the ProfileClass attribute
        private void Update()
        {
            DoSomeWork();
            
            if (_enableManualProfiling)
            {
                ManuallyProfiledMethod();
            }
        }
        
        // This method has its own profiling attribute
        [ProfileMethod(""Gameplay"", ""HeavyWork"")]
        private void DoSomeWork()
        {
            // Simulate some work
            for (int i = 0; i < 1000; i++)
            {
                Vector3.Distance(transform.position, Vector3.zero);
            }
        }
        
        // Example of manual profiling
        private void ManuallyProfiledMethod()
        {
            _profiledGameObject?.ProfileAction(""ManualWork"", () =>
            {
                // Some work that we want to profile manually
                System.Threading.Thread.Sleep(1);
            });
        }
        
        // This method won't be profiled due to the DoNotProfile attribute
        [DoNotProfile]
        private void InternalMethod()
        {
            // This won't be profiled
        }
    }
}";
            
            string filePath = Path.Combine(path, "ExampleProfiledBehaviour.cs");
            File.WriteAllText(filePath, scriptContent);
        }
        
        private void CreateExampleManagerScript(string path)
        {
            string scriptContent = @"using UnityEngine;
using AhBearStudios.Core.Profiling.Unity;
using Unity.Profiling;

namespace Examples.Profiling
{
    /// <summary>
    /// Example script showing how to use the ProfileManager directly
    /// </summary>
    public class ExampleProfilingManager : MonoBehaviour
    {
        [Header(""Profiling Controls"")]
        [SerializeField] private KeyCode _startProfilingKey = KeyCode.F1;
        [SerializeField] private KeyCode _stopProfilingKey = KeyCode.F2;
        [SerializeField] private KeyCode _resetStatsKey = KeyCode.F4;
        
        private ProfileManager _profileManager;
        
        private void Start()
        {
            _profileManager = ProfileManager.Instance;
            
            if (_profileManager != null)
            {
                // Subscribe to profiling events
                _profileManager.ProfilingStarted += OnProfilingStarted;
                _profileManager.ProfilingStopped += OnProfilingStopped;
                
                Debug.Log(""Example Profiling Manager initialized"");
            }
        }
        
        private void Update()
        {
            HandleInput();
            
            // Example of manual profiling scope
            using (_profileManager?.BeginScope(ProfilerCategory.Scripts, ""ExampleUpdate""))
            {
                DoUpdateWork();
            }
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(_startProfilingKey))
            {
                _profileManager?.StartProfiling();
            }
            
            if (Input.GetKeyDown(_stopProfilingKey))
            {
                _profileManager?.StopProfiling();
            }
            
            if (Input.GetKeyDown(_resetStatsKey))
            {
                _profileManager?.ResetStats();
            }
        }
        
        private void DoUpdateWork()
        {
            // Example work that will be profiled
            for (int i = 0; i < 100; i++)
            {
                transform.Rotate(0, 1, 0);
            }
        }
        
        private void OnProfilingStarted(ProfileManager profileManager)
        {
            Debug.Log(""Profiling started!"");
        }
        
        private void OnProfilingStopped(ProfileManager profileManager)
        {
            Debug.Log(""Profiling stopped!"");
        }
        
        private void OnDestroy()
        {
            if (_profileManager != null)
            {
                _profileManager.ProfilingStarted -= OnProfilingStarted;
                _profileManager.ProfilingStopped -= OnProfilingStopped;
            }
        }
    }
}";
            
            string filePath = Path.Combine(path, "ExampleProfilingManager.cs");
            File.WriteAllText(filePath, scriptContent);
        }
    }
}
#endif
# Character Controller System - HTN-Based Architecture Design Document

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Architectural Philosophy](#architectural-philosophy)
3. [Package Structure & Organization](#package-structure--organization)
4. [HTN Movement System](#htn-movement-system)
5. [Network Prediction Architecture (FishNet)](#network-prediction-architecture-fishnet)
6. [Motion Matching Integration (MxM)](#motion-matching-integration-mxm)
7. [Performance Architecture (Jobs & Burst)](#performance-architecture-jobs--burst)
8. [Core Services Integration](#core-services-integration)
9. [Component Hierarchy](#component-hierarchy)
10. [Implementation Roadmap](#implementation-roadmap)
11. [Code Examples](#code-examples)

---

## Executive Summary

### Vision
A **HTN-based character controller** that provides unified movement semantics for both player and AI characters, with deterministic network prediction, fluid motion matching animations, and high-performance Unity Jobs integration.

### Key Architectural Decisions

1. **HTN-First Design**: All movement uses Tasks, Conditions, Effects, and Operators
2. **Dual Package Architecture**: Platform-agnostic core + Unity-specific implementations
3. **Network-Deterministic**: FishNet prediction with proper rollback handling
4. **Trajectory-Centric**: Movement prediction drives animation selection
5. **Performance-Optimized**: Unity Jobs, Burst compilation, zero-allocation patterns
6. **Service-Integrated**: Full integration with AhBearStudios core services

### Design Principles

- **Declarative Movement**: Specify goals, let planner determine execution
- **Unified Semantics**: Same language for Player and AI
- **Network-Deterministic**: Identical outcomes on all clients
- **Animation-Driven**: Smooth visual presentation independent of physics rollback
- **Performance-First**: 60+ FPS with 100+ networked characters
- **Developer-Friendly**: Simple ability creation, easy debugging

---

## Architectural Philosophy

### From Imperative to Declarative

**Traditional Approach (Imperative)**:
```csharp
if (Input.GetButtonDown("Jump") && IsGrounded && HasStamina(10f))
{
    ApplyForce(Vector3.up * jumpForce);
    ConsumeStamina(10f);
    PlayAnimation("Jump");
}
```

**HTN Approach (Declarative)**:
```csharp
// Player creates goals
var goals = new[] { new JumpGoal() };

// Planner finds tasks to achieve goals
var plan = _planner.CreatePlan(context, goals);

// Execute plan using available tasks
plan.Execute(context);
```

### Unified Player/AI Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Goal Generation                      │
│    PlayerInputProvider │ AIInputProvider │ Network      │
├─────────────────────────────────────────────────────────┤
│                   HTN Planner                          │
│         Task Selection & Plan Generation                │
├─────────────────────────────────────────────────────────┤
│                 Task Execution                          │
│    Operators │ Methods │ Conditions │ Effects           │
├─────────────────────────────────────────────────────────┤
│              Deterministic Physics                      │
│        (Network Prediction & Reconciliation)           │
├─────────────────────────────────────────────────────────┤
│                Visual Presentation                      │
│           (Motion Matching & Animation)                │
└─────────────────────────────────────────────────────────┘
```

---

## Package Structure & Organization

### Core Package Architecture
Following AhBearStudios functional domain organization:

```
Assets/com.ahbearstudios.core/CharacterController/
├── AhBearStudios.Core.CharacterController.asmdef
├── ICharacterControllerService.cs           // Primary interface
├── CharacterControllerService.cs            // Primary implementation
├── Models/
│   ├── MovementContext.cs                   // HTN Context implementation
│   ├── CharacterState.cs                    // Network state (MemoryPack)
│   ├── MovementTask.cs                      // Base task implementation
│   └── MovementGoal.cs                      // Goal definitions
├── Tasks/                                   // HTN Task implementations
│   ├── Operators/                           // Primitive tasks
│   │   ├── JumpOperator.cs
│   │   ├── MoveOperator.cs
│   │   ├── RotateOperator.cs
│   │   └── StaminaOperator.cs
│   ├── Methods/                             // Compound tasks
│   │   ├── SprintJumpMethod.cs
│   │   ├── ClimbMethod.cs
│   │   └── SwimMethod.cs
│   └── Goals/                               // Goal definitions
│       ├── MoveToGoal.cs
│       ├── JumpGoal.cs
│       └── InteractGoal.cs
├── Conditions/                              // HTN Conditions
│   ├── IsGroundedCondition.cs
│   ├── HasStaminaCondition.cs
│   ├── CooldownCondition.cs
│   └── NetworkValidationCondition.cs
├── Effects/                                 // HTN Effects
│   ├── ApplyVelocityEffect.cs
│   ├── ConsumeStaminaEffect.cs
│   ├── TriggerAnimationEffect.cs
│   └── SendNetworkEventEffect.cs
├── Planning/                                // HTN Planning system
│   ├── IMovementPlanner.cs
│   ├── MovementPlanner.cs
│   ├── TaskSelector.cs
│   └── PlanValidator.cs
├── Physics/                                 // Core physics (deterministic)
│   ├── DeterministicMotor.cs
│   ├── CollisionDetection.cs
│   ├── GroundDetection.cs
│   └── MovementIntegration.cs
├── Configs/                                 // Configuration objects
│   ├── CharacterControllerConfig.cs
│   ├── MovementAbilityConfig.cs
│   └── NetworkPredictionConfig.cs
├── Builders/                                // Builder pattern
│   ├── CharacterControllerConfigBuilder.cs
│   ├── MovementPlannerBuilder.cs
│   └── TaskRegistryBuilder.cs
├── Factories/                               // Factory pattern
│   ├── CharacterControllerServiceFactory.cs
│   ├── MovementTaskFactory.cs
│   └── HTNPlannerFactory.cs
└── Messages/                                // Network messages
    ├── CharacterMovementMessage.cs
    ├── TaskExecutedMessage.cs
    └── PlanChangedMessage.cs
```

### Unity Package Architecture

```
Assets/com.ahbearstudios.unity/CharacterController/
├── AhBearStudios.Unity.CharacterController.asmdef
├── ICharacterControllerManager.cs           // Unity interface
├── CharacterControllerManager.cs            // MonoBehaviour implementation
├── Components/                              // MonoBehaviour components
│   ├── NetworkCharacterController.cs        // FishNet NetworkBehaviour
│   ├── CharacterPresenter.cs               // Visual interpolation
│   ├── TrajectoryVisualizer.cs             // Debug visualization
│   └── CharacterInputHandler.cs            // Input collection
├── Network/                                 // FishNet integration
│   ├── CharacterReplicateData.cs           // IReplicateData implementation
│   ├── CharacterReconcileData.cs           // IReconcileData implementation
│   ├── NetworkMovementValidator.cs         // Server-side validation
│   └── PredictionManager.cs                // Client prediction
├── Animation/                               // MxM integration
│   ├── DualTrajectorySystem.cs             // Network + Visual trajectories
│   ├── MxMIntegrationManager.cs            // MxM coordination
│   ├── AnimationEventSynchronizer.cs       // Network event timing
│   └── RootMotionHandler.cs                // Root motion management
├── Input/                                   // Input providers
│   ├── PlayerInputProvider.cs              // Human input
│   ├── AIInputProvider.cs                  // AI-driven input
│   ├── NetworkInputProvider.cs             // Replicated input
│   └── ReplayInputProvider.cs              // Recording/playback
├── Jobs/                                    // Unity Jobs integration
│   ├── CharacterMovementJob.cs             // Parallel movement processing
│   ├── CollisionDetectionJob.cs            // Parallel collision detection
│   ├── TrajectoryPredictionJob.cs          // Trajectory calculation
│   └── PlanExecutionJob.cs                 // HTN plan execution
├── Configs/                                 // ScriptableObject configs
│   ├── CharacterControllerAsset.cs         // Designer-friendly config
│   ├── MovementAbilityAsset.cs             // Ability configurations
│   └── NetworkSettingsAsset.cs             // Network parameters
├── Installers/                              // Reflex installers
│   ├── CharacterControllerInstaller.cs
│   └── MovementTaskInstaller.cs
└── Editor/                                  // Editor tools
    ├── CharacterControllerInspector.cs
    ├── MovementAbilityEditor.cs
    └── NetworkDebuggingWindow.cs
```

---

## HTN Movement System

### Core HTN Components

#### Movement Context (HTN World State)

The movement context serves as the HTN world state, containing all information needed for planning and execution:

```csharp
namespace AhBearStudios.Core.CharacterController.Models
{
    /// <summary>
    /// HTN Context implementation for character movement.
    /// Contains all world state information needed for task planning and execution.
    /// Integrates with AhBearStudios core services and follows IMessage patterns.
    /// </summary>
    public class MovementContext : IContext
    {
        #region World State Properties
        
        // Physics State (Authoritative)
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        
        // Character State
        public bool IsGrounded { get; set; }
        public bool IsStable { get; set; }
        public float StabilityScore { get; set; }
        public Vector3 GroundNormal { get; set; }
        public Collider GroundCollider { get; set; }
        
        // Resources
        public float Stamina { get; set; }
        public float Health { get; set; }
        public float Speed { get; set; }
        
        // Network State
        public uint CurrentTick { get; set; }
        public bool IsAuthoritative { get; set; }
        public uint LastValidatedTick { get; set; }
        
        #endregion
        
        #region HTN Blackboard
        
        private readonly Dictionary<string, object> _blackboard = new();
        
        public T GetValue<T>(string key)
        {
            return _blackboard.ContainsKey(key) ? (T)_blackboard[key] : default;
        }
        
        public void SetValue<T>(string key, T value)
        {
            _blackboard[key] = value;
        }
        
        public bool HasValue(string key) => _blackboard.ContainsKey(key);
        
        #endregion
        
        #region Service References
        
        public ISerializationService SerializationService { get; set; }
        public IMessageBusService MessageBus { get; set; }
        public ILoggingService Logger { get; set; }
        public IPoolingService PoolingService { get; set; }
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Creates a movement context using AhBearStudios builder pattern.
        /// </summary>
        public static MovementContext Create(
            ISerializationService serialization,
            IMessageBusService messageBus,
            ILoggingService logger,
            IPoolingService pooling)
        {
            return new MovementContext
            {
                SerializationService = serialization ?? throw new ArgumentNullException(nameof(serialization)),
                MessageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus)),
                Logger = logger ?? throw new ArgumentNullException(nameof(logger)),
                PoolingService = pooling ?? throw new ArgumentNullException(nameof(pooling)),
                CurrentTick = 0,
                IsAuthoritative = false,
                Stamina = 100f,
                Health = 100f,
                Speed = 1f
            };
        }
        
        #endregion
    }
}
```

#### HTN Task Interface

All movement actions implement the unified task interface:

```csharp
namespace AhBearStudios.Core.CharacterController.Tasks
{
    /// <summary>
    /// Base interface for all movement tasks in the HTN system.
    /// Provides unified execution semantics for Player and AI characters.
    /// Integrates with FishNet prediction and MxM animation systems.
    /// </summary>
    public interface IMovementTask : ITask
    {
        #region Identification
        
        /// <summary>
        /// Unique task identifier for network synchronization.
        /// </summary>
        ushort TaskId { get; }
        
        /// <summary>
        /// Human-readable task name for debugging.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Task category for organization and filtering.
        /// </summary>
        TaskCategory Category { get; }
        
        #endregion
        
        #region HTN Execution
        
        /// <summary>
        /// Conditions that must be met for task execution.
        /// </summary>
        IReadOnlyList<ICondition> Preconditions { get; }
        
        /// <summary>
        /// Effects applied after successful execution.
        /// </summary>
        IReadOnlyList<IEffect> Effects { get; }
        
        /// <summary>
        /// Execute the task with the given context.
        /// </summary>
        TaskStatus Execute(MovementContext context);
        
        /// <summary>
        /// Validate task execution on the server.
        /// </summary>
        bool ValidateExecution(MovementContext serverContext);
        
        #endregion
        
        #region Network Prediction
        
        /// <summary>
        /// Can this task be predicted client-side?
        /// </summary>
        bool IsPredictable { get; }
        
        /// <summary>
        /// Should this task be cleared during future prediction?
        /// </summary>
        bool ClearOnPredict { get; }
        
        /// <summary>
        /// Priority for task selection (higher = more important).
        /// </summary>
        uint Priority { get; }
        
        #endregion
        
        #region Animation Integration
        
        /// <summary>
        /// Contribute to trajectory prediction for MxM.
        /// </summary>
        void ContributeToTrajectory(ITrajectoryProvider trajectory, float deltaTime);
        
        /// <summary>
        /// Animation tags required for this task.
        /// </summary>
        string[] RequiredAnimationTags { get; }
        
        /// <summary>
        /// MxM event triggered by this task (if any).
        /// </summary>
        int? AnimationEventId { get; }
        
        #endregion
    }
}
```

#### Task Categories

```csharp
namespace AhBearStudios.Core.CharacterController.Models
{
    /// <summary>
    /// Categories for organizing movement tasks.
    /// Used for filtering and priority management.
    /// </summary>
    public enum TaskCategory : byte
    {
        // Basic Movement
        Locomotion = 0,     // Walk, run, sprint
        Rotation = 1,       // Turn, face direction
        
        // Vertical Movement  
        Jump = 10,          // Jump, double jump
        Fall = 11,          // Falling, landing
        Climb = 12,         // Climb, vault
        
        // Special Movement
        Swim = 20,          // Swimming actions
        Slide = 21,         // Sliding, crouching
        Dash = 22,          // Dash, dodge
        
        // Combat Movement
        Attack = 30,        // Attack combos
        Block = 31,         // Defensive actions
        Evade = 32,         // Evasive maneuvers
        
        // Interaction
        Interact = 40,      // Object interaction
        Pickup = 41,        // Item pickup
        Use = 42,           // Use items/abilities
        
        // AI Specific
        Patrol = 50,        // AI patrol behavior
        Pursue = 51,        // AI chase behavior
        Investigate = 52,   // AI investigation
        
        // System
        Recovery = 60,      // Error recovery
        Maintenance = 61    // System maintenance
    }
}
```

---

## Network Prediction Architecture (FishNet)

### HTN-Aware Network Replication

The character controller integrates HTN planning with FishNet's tick-based prediction system:

#### Replicate Data Structure

```csharp
namespace AhBearStudios.Unity.CharacterController.Network
{
    /// <summary>
    /// Network input data for character movement replication.
    /// Follows FishNet IReplicateData pattern with HTN goal integration.
    /// Uses ISerializationService for compression.
    /// </summary>
    [MemoryPackable]
    public struct CharacterReplicateData : IReplicateData
    {
        #region Input Data
        
        /// <summary>
        /// Movement input vector (WASD normalized).
        /// </summary>
        public Vector2 MoveInput { get; set; }
        
        /// <summary>
        /// Look input for rotation.
        /// </summary>
        public Vector2 LookInput { get; set; }
        
        /// <summary>
        /// Packed action flags (Jump, Sprint, Crouch, etc.).
        /// Uses bit manipulation for bandwidth efficiency.
        /// </summary>
        public ushort ActionFlags { get; set; }
        
        /// <summary>
        /// HTN goals requested this tick.
        /// Serialized using ISerializationService.
        /// </summary>
        public byte[] SerializedGoals { get; set; }
        
        #endregion
        
        #region One-Time Inputs
        
        /// <summary>
        /// Actions that should only execute once.
        /// Reset after processing to prevent repeated execution.
        /// </summary>
        public struct OneTimeInputs
        {
            public bool Jump;
            public bool Interact;
            public bool UseItem;
            public byte AbilitySlot; // 0-255 ability slots
            
            public void Reset()
            {
                Jump = false;
                Interact = false;
                UseItem = false;
                AbilitySlot = 0;
            }
        }
        
        public OneTimeInputs OneTime { get; set; }
        
        #endregion
        
        #region FishNet Implementation
        
        private uint _tick;
        
        public void Dispose() 
        { 
            OneTime.Reset();
            SerializedGoals = null;
        }
        
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Creates replicate data from HTN goals using AhBearStudios patterns.
        /// </summary>
        public static CharacterReplicateData Create(
            Vector2 moveInput,
            Vector2 lookInput,
            ushort actionFlags,
            IMovementGoal[] goals,
            OneTimeInputs oneTime,
            ISerializationService serialization)
        {
            var serializedGoals = serialization?.Serialize(goals) ?? Array.Empty<byte>();
            
            return new CharacterReplicateData
            {
                MoveInput = moveInput,
                LookInput = lookInput,
                ActionFlags = actionFlags,
                SerializedGoals = serializedGoals,
                OneTime = oneTime,
                _tick = 0 // Set by FishNet
            };
        }
        
        #endregion
    }
}
```

#### Reconcile Data Structure

```csharp
namespace AhBearStudios.Unity.CharacterController.Network
{
    /// <summary>
    /// Authoritative state data for client reconciliation.
    /// Contains deterministic physics state and HTN plan validation.
    /// </summary>
    [MemoryPackable]
    public struct CharacterReconcileData : IReconcileData
    {
        #region Transform State
        
        /// <summary>
        /// Character position (local space if parented).
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// Character rotation.
        /// </summary>
        public Quaternion Rotation { get; set; }
        
        /// <summary>
        /// Physics velocity.
        /// </summary>
        public Vector3 Velocity { get; set; }
        
        /// <summary>
        /// Vertical velocity component for jump/fall prediction.
        /// </summary>
        public float VerticalVelocity { get; set; }
        
        #endregion
        
        #region Character State
        
        /// <summary>
        /// Grounding status for movement validation.
        /// </summary>
        public bool IsGrounded { get; set; }
        
        /// <summary>
        /// Ground normal for slope calculations.
        /// </summary>
        public Vector3 GroundNormal { get; set; }
        
        /// <summary>
        /// Current stamina for ability validation.
        /// </summary>
        public float Stamina { get; set; }
        
        /// <summary>
        /// Current movement speed multiplier.
        /// </summary>
        public float SpeedMultiplier { get; set; }
        
        #endregion
        
        #region Network State
        
        /// <summary>
        /// Attached platform for moving platform support.
        /// </summary>
        public uint AttachedPlatformId { get; set; }
        
        /// <summary>
        /// Last validated HTN plan hash for plan synchronization.
        /// </summary>
        public uint PlanHash { get; set; }
        
        /// <summary>
        /// Active animation state for MxM synchronization.
        /// </summary>
        public ushort AnimationStateId { get; set; }
        
        #endregion
        
        #region FishNet Implementation
        
        private uint _tick;
        
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// Creates reconcile data from current character state.
        /// </summary>
        public static CharacterReconcileData Create(
            MovementContext context,
            Transform transform,
            uint attachedPlatformId = 0,
            uint planHash = 0,
            ushort animationStateId = 0)
        {
            return new CharacterReconcileData
            {
                Position = transform.localPosition,
                Rotation = transform.localRotation,
                Velocity = context.Velocity,
                VerticalVelocity = context.Velocity.y,
                IsGrounded = context.IsGrounded,
                GroundNormal = context.GroundNormal,
                Stamina = context.Stamina,
                SpeedMultiplier = context.Speed,
                AttachedPlatformId = attachedPlatformId,
                PlanHash = planHash,
                AnimationStateId = animationStateId,
                _tick = context.CurrentTick
            };
        }
        
        #endregion
    }
}
```

#### Network Character Controller

```csharp
namespace AhBearStudios.Unity.CharacterController.Components
{
    /// <summary>
    /// FishNet NetworkBehaviour that integrates HTN planning with network prediction.
    /// Follows AhBearStudios architecture patterns and service integration.
    /// </summary>
    public class NetworkCharacterController : TickNetworkBehaviour
    {
        #region Dependencies
        
        [Inject] private ICharacterControllerService _characterService;
        [Inject] private ISerializationService _serializationService;
        [Inject] private IMessageBusService _messageBus;
        [Inject] private ILoggingService _logger;
        [Inject] private IPoolingService _pooling;
        
        private IMovementPlanner _planner;
        private MovementContext _context;
        
        #endregion
        
        #region Prediction State
        
        private CharacterReplicateData _lastTickedReplicateData;
        private CharacterReconcileData _lastReconcileData;
        private Queue<IMovementGoal> _goalBuffer = new();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Create context with injected services
            _context = MovementContext.Create(
                _serializationService,
                _messageBus,
                _logger,
                _pooling);
        }
        
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            
            // Initialize planner with task registry
            var plannerBuilder = new MovementPlannerBuilder(_pooling)
                .RegisterDefaultTasks()
                .SetContext(_context);
                
            _planner = plannerBuilder.Build();
            
            _logger.LogInfo($"NetworkCharacterController started for {(IsOwner ? "Owner" : "Remote")}");
        }
        
        #endregion
        
        #region Input Processing
        
        private CharacterReplicateData BuildInputData()
        {
            if (!IsOwner) return default;
            
            // Get input from provider
            var inputProvider = _context.GetValue<IInputProvider>("InputProvider");
            var goals = inputProvider.GetGoals();
            
            // Build one-time inputs
            var oneTime = new CharacterReplicateData.OneTimeInputs
            {
                Jump = inputProvider.GetButtonDown("Jump"),
                Interact = inputProvider.GetButtonDown("Interact"),
                UseItem = inputProvider.GetButtonDown("UseItem"),
                AbilitySlot = inputProvider.GetAbilitySlot()
            };
            
            return CharacterReplicateData.Create(
                inputProvider.MovementInput,
                inputProvider.LookInput,
                inputProvider.GetPackedActions(),
                goals,
                oneTime,
                _serializationService);
        }
        
        #endregion
        
        #region FishNet Replication
        
        [Replicate]
        private void ProcessMovement(
            CharacterReplicateData data, 
            ReplicateState state = ReplicateState.Invalid, 
            Channel channel = Channel.Unreliable)
        {
            float deltaTime = (float)TimeManager.TickDelta;
            
            // Handle future prediction for non-owners
            if (!IsServerStarted && !IsOwner)
            {
                data = HandlePrediction(data, state);
            }
            
            // Deserialize goals
            var goals = DeserializeGoals(data.SerializedGoals);
            
            // Create or update HTN plan
            if (goals.Length > 0)
            {
                var plan = _planner.CreatePlan(_context, goals);
                ExecutePlan(plan, deltaTime);
            }
            
            // Apply physics integration
            _characterService.IntegrateMovement(_context, deltaTime);
            
            // Update transform
            transform.position = _context.Position;
            transform.rotation = _context.Rotation;
            
            // Send movement message
            if (IsOwner)
            {
                var message = CharacterMovementMessage.Create(
                    ObjectId,
                    TimeManager.LocalTick,
                    _context.Position,
                    _context.Velocity);
                _messageBus.PublishMessage(message);
            }
        }
        
        [Reconcile]
        private void ApplyReconciliation(
            CharacterReconcileData data,
            Channel channel = Channel.Unreliable)
        {
            // Restore authoritative state
            transform.localPosition = data.Position;
            transform.localRotation = data.Rotation;
            
            _context.Position = data.Position;
            _context.Velocity = data.Velocity;
            _context.IsGrounded = data.IsGrounded;
            _context.GroundNormal = data.GroundNormal;
            _context.Stamina = data.Stamina;
            _context.Speed = data.SpeedMultiplier;
            
            // Handle platform attachment
            HandlePlatformAttachment(data.AttachedPlatformId);
            
            // Cache for prediction fallback
            _lastReconcileData = data;
            
            _logger.LogDebug($"Reconciled character state at tick {data.GetTick()}");
        }
        
        public override void CreateReconcile()
        {
            var data = CharacterReconcileData.Create(
                _context,
                transform,
                GetAttachedPlatformId(),
                GetCurrentPlanHash(),
                GetAnimationStateId());
                
            ApplyReconciliation(data);
        }
        
        #endregion
        
        #region Prediction Handling
        
        private CharacterReplicateData HandlePrediction(
            CharacterReplicateData data, 
            ReplicateState state)
        {
            if (state.ContainsTicked())
            {
                // Cache real input data
                _lastTickedReplicateData.Dispose();
                _lastTickedReplicateData = data;
                return data;
            }
            
            if (state.IsFuture())
            {
                // Predict up to 3 ticks ahead
                if (data.GetTick() - _lastTickedReplicateData.GetTick() > 3)
                {
                    // Too far in future, use minimal prediction
                    return CreateMinimalPrediction();
                }
                
                // Use last known input for prediction
                data.Dispose();
                data = _lastTickedReplicateData;
                
                // Clear one-time inputs for prediction
                data.OneTime.Reset();
            }
            
            return data;
        }
        
        #endregion
        
        #region HTN Plan Execution
        
        private void ExecutePlan(IMovementPlan plan, float deltaTime)
        {
            foreach (var task in plan.GetTasks())
            {
                var status = task.Execute(_context);
                
                if (status == TaskStatus.Failed)
                {
                    _logger.LogWarning($"Task {task.Name} failed, replanning...");
                    
                    // Send task failure message
                    var message = TaskFailedMessage.Create(
                        task.TaskId,
                        TimeManager.LocalTick,
                        "Task execution failed");
                    _messageBus.PublishMessage(message);
                    
                    break;
                }
            }
        }
        
        private IMovementGoal[] DeserializeGoals(byte[] serializedData)
        {
            if (serializedData == null || serializedData.Length == 0)
                return Array.Empty<IMovementGoal>();
                
            return _serializationService.Deserialize<IMovementGoal[]>(serializedData);
        }
        
        #endregion
    }
}
```

---

## Motion Matching Integration (MxM)

### Dual Trajectory System Architecture

The key insight for MxM integration is **separating network determinism from visual quality**:

- **Network Trajectory**: Low-frequency (10-15Hz), deterministic, used for physics
- **Visual Trajectory**: High-frequency (30-60Hz), interpolated, used for animation

#### Dual Trajectory Manager

```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Manages dual trajectory system for MxM integration.
    /// Separates network-deterministic trajectory from smooth visual trajectory.
    /// Integrates with HTN planning and FishNet prediction.
    /// </summary>
    public class DualTrajectorySystem : MonoBehaviour
    {
        #region Configuration
        
        [Header("Network Trajectory")]
        [SerializeField] private int _networkSampleCount = 8;
        [SerializeField] private float _networkSampleInterval = 0.1f;
        [SerializeField] private bool _compressNetworkTrajectory = true;
        
        [Header("Visual Trajectory")]
        [SerializeField] private int _visualSampleCount = 12;
        [SerializeField] private float _visualSampleInterval = 0.083f; // 60Hz
        [SerializeField] private float _smoothingFactor = 0.85f;
        
        #endregion
        
        #region Dependencies
        
        [Inject] private ISerializationService _serialization;
        [Inject] private IPoolingService _pooling;
        [Inject] private ILoggingService _logger;
        
        private NetworkCharacterController _networkController;
        private MovementContext _context;
        private IMovementPlanner _planner;
        
        #endregion
        
        #region Trajectory Data
        
        // Network trajectory (deterministic, low frequency)
        private readonly CircularBuffer<TrajectoryPoint> _networkTrajectory;
        private readonly CircularBuffer<TrajectoryPoint> _networkPrediction;
        
        // Visual trajectory (smooth, high frequency)  
        private readonly CircularBuffer<TrajectoryPoint> _visualTrajectory;
        private readonly CircularBuffer<TrajectoryPoint> _visualPrediction;
        
        // MxM interface
        private MxMTrajectoryGenerator _mxmGenerator;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _networkTrajectory = new CircularBuffer<TrajectoryPoint>(_networkSampleCount * 2);
            _networkPrediction = new CircularBuffer<TrajectoryPoint>(_networkSampleCount);
            _visualTrajectory = new CircularBuffer<TrajectoryPoint>(_visualSampleCount * 2);
            _visualPrediction = new CircularBuffer<TrajectoryPoint>(_visualSampleCount);
            
            _networkController = GetComponent<NetworkCharacterController>();
            _mxmGenerator = GetComponent<MxMTrajectoryGenerator>();
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // Update visual trajectory (high frequency)
            UpdateVisualTrajectory(deltaTime);
            
            // Update MxM trajectory
            UpdateMxMTrajectory();
        }
        
        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            
            // Update network trajectory (network frequency)
            if (ShouldUpdateNetworkTrajectory())
            {
                UpdateNetworkTrajectory(deltaTime);
            }
        }
        
        #endregion
        
        #region Network Trajectory Updates
        
        private void UpdateNetworkTrajectory(float deltaTime)
        {
            // Get current HTN plan from network controller
            var currentPlan = _planner.GetCurrentPlan();
            
            // Record past trajectory point
            var pastPoint = new TrajectoryPoint
            {
                Position = _context.Position,
                Velocity = _context.Velocity,
                Rotation = _context.Rotation,
                Timestamp = Time.fixedTime
            };
            
            _networkTrajectory.Add(pastPoint);
            
            // Predict future trajectory from HTN plan
            PredictNetworkTrajectory(currentPlan, deltaTime);
            
            _logger.LogDebug($"Updated network trajectory with {_networkPrediction.Count} future points");
        }
        
        private void PredictNetworkTrajectory(IMovementPlan plan, float deltaTime)
        {
            _networkPrediction.Clear();
            
            // Clone context for prediction
            var predictiveContext = _context.Clone();
            var time = 0f;
            
            for (int i = 0; i < _networkSampleCount; i++)
            {
                time += _networkSampleInterval;
                
                // Execute plan tasks to predict movement
                foreach (var task in plan.GetTasks())
                {
                    task.ContributeToTrajectory(this, deltaTime);
                }
                
                // Apply predicted movement
                predictiveContext.Position += predictiveContext.Velocity * _networkSampleInterval;
                
                var predictedPoint = new TrajectoryPoint
                {
                    Position = predictiveContext.Position,
                    Velocity = predictiveContext.Velocity,
                    Rotation = predictiveContext.Rotation,
                    Timestamp = Time.fixedTime + time
                };
                
                _networkPrediction.Add(predictedPoint);
            }
        }
        
        #endregion
        
        #region Visual Trajectory Updates
        
        private void UpdateVisualTrajectory(float deltaTime)
        {
            // Interpolate from network trajectory to create smooth visual trajectory
            InterpolateVisualTrajectory();
            
            // Predict visual future based on current visual state
            PredictVisualTrajectory(deltaTime);
        }
        
        private void InterpolateVisualTrajectory()
        {
            if (_networkTrajectory.Count < 2) return;
            
            var currentTime = Time.time;
            var networkPoint1 = _networkTrajectory[_networkTrajectory.Count - 2];
            var networkPoint2 = _networkTrajectory[_networkTrajectory.Count - 1];
            
            // Calculate interpolation factor based on time
            var timeDelta = networkPoint2.Timestamp - networkPoint1.Timestamp;
            var factor = timeDelta > 0 ? (currentTime - networkPoint1.Timestamp) / timeDelta : 0f;
            factor = Mathf.Clamp01(factor);
            
            // Smooth interpolation for visual trajectory
            var smoothFactor = Mathf.Lerp(factor, 1f, _smoothingFactor * Time.deltaTime);
            
            var visualPoint = new TrajectoryPoint
            {
                Position = Vector3.Lerp(networkPoint1.Position, networkPoint2.Position, smoothFactor),
                Velocity = Vector3.Lerp(networkPoint1.Velocity, networkPoint2.Velocity, smoothFactor),
                Rotation = Quaternion.Slerp(networkPoint1.Rotation, networkPoint2.Rotation, smoothFactor),
                Timestamp = currentTime
            };
            
            _visualTrajectory.Add(visualPoint);
        }
        
        private void PredictVisualTrajectory(float deltaTime)
        {
            _visualPrediction.Clear();
            
            if (_visualTrajectory.Count == 0) return;
            
            var lastVisualPoint = _visualTrajectory[_visualTrajectory.Count - 1];
            var time = 0f;
            
            for (int i = 0; i < _visualSampleCount; i++)
            {
                time += _visualSampleInterval;
                
                // Simple kinematic prediction for visual smoothness
                var predictedPosition = lastVisualPoint.Position + lastVisualPoint.Velocity * time;
                
                var predictedPoint = new TrajectoryPoint
                {
                    Position = predictedPosition,
                    Velocity = lastVisualPoint.Velocity,
                    Rotation = lastVisualPoint.Rotation,
                    Timestamp = lastVisualPoint.Timestamp + time
                };
                
                _visualPrediction.Add(predictedPoint);
            }
        }
        
        #endregion
        
        #region MxM Integration
        
        private void UpdateMxMTrajectory()
        {
            if (_mxmGenerator == null) return;
            
            // Use visual trajectory for MxM (smooth animation)
            var trajectoryPoints = GetMxMTrajectoryPoints();
            _mxmGenerator.SetCustomTrajectory(trajectoryPoints);
        }
        
        private Vector3[] GetMxMTrajectoryPoints()
        {
            var points = new Vector3[_visualSampleCount];
            
            for (int i = 0; i < _visualSampleCount && i < _visualPrediction.Count; i++)
            {
                points[i] = _visualPrediction[i].Position;
            }
            
            return points;
        }
        
        #endregion
        
        #region HTN Trajectory Contribution
        
        /// <summary>
        /// Called by HTN tasks to contribute to trajectory prediction.
        /// </summary>
        public void ModifyTrajectoryPrediction(Vector3 velocityDelta, float influence)
        {
            if (_networkPrediction.Count > 0)
            {
                var lastPoint = _networkPrediction[_networkPrediction.Count - 1];
                lastPoint.Velocity += velocityDelta * influence;
                _networkPrediction[_networkPrediction.Count - 1] = lastPoint;
            }
        }
        
        /// <summary>
        /// Apply trajectory modification from jump prediction.
        /// </summary>
        public void PredictJumpTrajectory(Vector3 initialVelocity, float gravity)
        {
            for (int i = 0; i < _networkPrediction.Count; i++)
            {
                var point = _networkPrediction[i];
                var time = (point.Timestamp - Time.fixedTime);
                
                // Kinematic jump prediction
                point.Velocity = new Vector3(
                    initialVelocity.x,
                    initialVelocity.y + gravity * time,
                    initialVelocity.z);
                    
                _networkPrediction[i] = point;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Individual trajectory sample point.
    /// </summary>
    [System.Serializable]
    public struct TrajectoryPoint
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public float Timestamp;
    }
}
```

#### MxM Animation Event Synchronizer

```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Synchronizes MxM animation events with network ticks.
    /// Ensures animation events are properly timed with network prediction.
    /// </summary>
    public class AnimationEventSynchronizer : MonoBehaviour
    {
        #region Dependencies
        
        [Inject] private IMessageBusService _messageBus;
        [Inject] private ILoggingService _logger;
        
        private NetworkCharacterController _networkController;
        private MxMAnimator _mxmAnimator;
        
        #endregion
        
        #region Event Queue
        
        private struct QueuedAnimationEvent
        {
            public int EventId;
            public uint TargetTick;
            public float LocalTime;
            public Vector3[] Contacts;
        }
        
        private Queue<QueuedAnimationEvent> _pendingEvents = new();
        private Dictionary<int, MxMEventDefinition> _eventDefinitions = new();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _networkController = GetComponent<NetworkCharacterController>();
            _mxmAnimator = GetComponent<MxMAnimator>();
            
            // Subscribe to MxM events
            if (_mxmAnimator != null)
            {
                _mxmAnimator.onIdleTriggered.AddListener(OnMxMEvent);
                _mxmAnimator.onEventComplete.AddListener(OnMxMEventComplete);
            }
        }
        
        private void Update()
        {
            ProcessQueuedEvents();
        }
        
        #endregion
        
        #region Event Processing
        
        private void OnMxMEvent(MxMEventDefinition eventDef)
        {
            // Queue event for next network tick
            var queuedEvent = new QueuedAnimationEvent
            {
                EventId = eventDef.EventId,
                TargetTick = _networkController.TimeManager.LocalTick + 1,
                LocalTime = Time.time,
                Contacts = eventDef.GetContacts()
            };
            
            _pendingEvents.Enqueue(queuedEvent);
            
            _logger.LogDebug($"Queued animation event {eventDef.name} for tick {queuedEvent.TargetTick}");
        }
        
        private void ProcessQueuedEvents()
        {
            var currentTick = _networkController.TimeManager.LocalTick;
            
            while (_pendingEvents.Count > 0)
            {
                var queuedEvent = _pendingEvents.Peek();
                
                if (queuedEvent.TargetTick <= currentTick)
                {
                    // Execute event at tick boundary
                    ExecuteAnimationEvent(queuedEvent);
                    _pendingEvents.Dequeue();
                }
                else
                {
                    break; // Wait for target tick
                }
            }
        }
        
        private void ExecuteAnimationEvent(QueuedAnimationEvent animEvent)
        {
            // Send animation event message
            var message = AnimationEventTriggeredMessage.Create(
                _networkController.ObjectId,
                animEvent.EventId,
                animEvent.TargetTick,
                animEvent.Contacts);
                
            _messageBus.PublishMessage(message);
            
            _logger.LogDebug($"Executed animation event {animEvent.EventId} at tick {animEvent.TargetTick}");
        }
        
        #endregion
    }
}
```

### Root Motion Integration

```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Handles root motion integration with HTN tasks and network prediction.
    /// </summary>
    public enum RootMotionMode
    {
        Disabled = 0,           // No root motion
        BlendWithPhysics = 1,   // Mix with physics simulation  
        OverridePhysics = 2,    // Root motion drives movement
        EventBased = 3          // Root motion only during specific events
    }
    
    public class RootMotionHandler : MonoBehaviour
    {
        [SerializeField] private RootMotionMode _rootMotionMode = RootMotionMode.BlendWithPhysics;
        [SerializeField] private float _blendFactor = 0.5f;
        [SerializeField] private string[] _rootMotionEvents = { "Climb", "Vault", "Attack" };
        
        private NetworkCharacterController _networkController;
        private MovementContext _context;
        private MxMAnimator _mxmAnimator;
        
        private Vector3 _rootMotionDelta;
        private Quaternion _rootMotionRotationDelta;
        
        private void OnAnimatorMove()
        {
            if (_networkController == null || !_networkController.IsOwner)
                return;
                
            _rootMotionDelta = _mxmAnimator.deltaPosition;
            _rootMotionRotationDelta = _mxmAnimator.deltaRotation;
            
            ApplyRootMotion();
        }
        
        private void ApplyRootMotion()
        {
            switch (_rootMotionMode)
            {
                case RootMotionMode.Disabled:
                    // Do nothing
                    break;
                    
                case RootMotionMode.BlendWithPhysics:
                    // Blend root motion with physics velocity
                    var physicsVelocity = _context.Velocity;
                    var rootVelocity = _rootMotionDelta / Time.deltaTime;
                    _context.Velocity = Vector3.Lerp(physicsVelocity, rootVelocity, _blendFactor);
                    break;
                    
                case RootMotionMode.OverridePhysics:
                    // Root motion completely overrides physics
                    _context.Position += _rootMotionDelta;
                    _context.Rotation *= _rootMotionRotationDelta;
                    break;
                    
                case RootMotionMode.EventBased:
                    // Only apply during specific animation events
                    if (IsRootMotionEventActive())
                    {
                        _context.Position += _rootMotionDelta;
                        _context.Rotation *= _rootMotionRotationDelta;
                    }
                    break;
            }
        }
    }
}
```

---

## Visual-Network Synchronization & Lag Compensation

The dual trajectory system introduces complex synchronization challenges when network reconciliation occurs. This section details strategies for maintaining visual quality while preserving network determinism.

### Core Challenge: Animation-Movement Desynchronization

When FishNet reconciliation corrects network predictions, the visual animation system can become desynchronized from the authoritative movement state. This creates several problems:

#### Problem 1: Animation State Conflicts
```
Frame 100: Player starts 1-second jump animation
Frame 105: Network reconciliation reveals player was stunned instead
Problem: Animation is 50% through jump, should be in stun state
```

#### Problem 2: Root Motion vs Network Authority
```
Network State: Character at position (10, 0, 5) after reconciliation
Visual State: Root motion moved character to (10.5, 0, 5.2)
Problem: Visual position conflicts with network authority
```

#### Problem 3: Animation Event Timing
```
Attack animation triggers "damage" event at 0.3 seconds
Network rollback changes attack timing by 100ms
Problem: Damage event executed at wrong time
```

### Animation State Checkpointing System

#### Animation State Snapshots
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Lightweight animation state checkpoint for rollback scenarios.
    /// Captures essential animation data without full Animator state complexity.
    /// </summary>
    [MemoryPackable]
    public struct AnimationCheckpoint
    {
        /// <summary>
        /// Network tick when checkpoint was created.
        /// </summary>
        public uint Tick { get; set; }
        
        /// <summary>
        /// Hash of the current animation state.
        /// </summary>
        public int StateHash { get; set; }
        
        /// <summary>
        /// Normalized progress through current animation (0-1).
        /// </summary>
        public float NormalizedTime { get; set; }
        
        /// <summary>
        /// Animation layer weights for blend trees.
        /// </summary>
        public float[] BlendWeights { get; set; }
        
        /// <summary>
        /// Root motion delta since last checkpoint.
        /// </summary>
        public Vector3 RootMotionDelta { get; set; }
        
        /// <summary>
        /// Animation speed multiplier at checkpoint time.
        /// </summary>
        public float SpeedMultiplier { get; set; }
        
        /// <summary>
        /// Create checkpoint from current MxM animator state.
        /// </summary>
        public static AnimationCheckpoint Create(
            uint tick,
            MxMAnimator animator,
            ISerializationService serialization)
        {
            return new AnimationCheckpoint
            {
                Tick = tick,
                StateHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash,
                NormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                BlendWeights = animator.GetLayerWeights(),
                RootMotionDelta = animator.deltaPosition,
                SpeedMultiplier = animator.speed
            };
        }
    }
    
    /// <summary>
    /// Manages animation state checkpointing for network rollback scenarios.
    /// </summary>
    public class AnimationStateManager : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField] private int _maxCheckpoints = 10;
        [SerializeField] private uint _checkpointInterval = 3; // Every 3 ticks
        
        #endregion
        
        #region Dependencies
        
        [Inject] private ISerializationService _serialization;
        [Inject] private ILoggingService _logger;
        
        private NetworkCharacterController _networkController;
        private MxMAnimator _mxmAnimator;
        
        #endregion
        
        #region Checkpoint Storage
        
        private readonly CircularBuffer<AnimationCheckpoint> _checkpoints;
        private uint _lastCheckpointTick = 0;
        
        #endregion
        
        private void Awake()
        {
            _checkpoints = new CircularBuffer<AnimationCheckpoint>(_maxCheckpoints);
            _networkController = GetComponent<NetworkCharacterController>();
            _mxmAnimator = GetComponent<MxMAnimator>();
        }
        
        private void Update()
        {
            if (_networkController.IsOwner)
            {
                CreateCheckpointIfNeeded();
            }
        }
        
        private void CreateCheckpointIfNeeded()
        {
            var currentTick = _networkController.TimeManager.LocalTick;
            
            if (currentTick - _lastCheckpointTick >= _checkpointInterval)
            {
                var checkpoint = AnimationCheckpoint.Create(
                    currentTick, 
                    _mxmAnimator, 
                    _serialization);
                
                _checkpoints.Add(checkpoint);
                _lastCheckpointTick = currentTick;
                
                _logger.LogDebug($"Created animation checkpoint at tick {currentTick}");
            }
        }
        
        /// <summary>
        /// Restore animation state to nearest checkpoint before given tick.
        /// </summary>
        public bool RestoreToTick(uint targetTick)
        {
            AnimationCheckpoint? bestCheckpoint = null;
            
            // Find checkpoint closest to but before target tick
            foreach (var checkpoint in _checkpoints)
            {
                if (checkpoint.Tick <= targetTick)
                {
                    if (bestCheckpoint == null || checkpoint.Tick > bestCheckpoint.Value.Tick)
                    {
                        bestCheckpoint = checkpoint;
                    }
                }
            }
            
            if (bestCheckpoint.HasValue)
            {
                RestoreAnimationState(bestCheckpoint.Value);
                return true;
            }
            
            return false;
        }
        
        private void RestoreAnimationState(AnimationCheckpoint checkpoint)
        {
            // Fast-forward from checkpoint to current state
            _mxmAnimator.Play(checkpoint.StateHash, 0, checkpoint.NormalizedTime);
            _mxmAnimator.speed = checkpoint.SpeedMultiplier;
            
            // Restore blend weights
            for (int i = 0; i < checkpoint.BlendWeights.Length; i++)
            {
                _mxmAnimator.SetLayerWeight(i, checkpoint.BlendWeights[i]);
            }
            
            _logger.LogDebug($"Restored animation state from tick {checkpoint.Tick}");
        }
    }
}
```

### Lag Compensation Strategies

#### Strategy 1: Animation Speed Adjustment
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Dynamically adjusts animation playback speed to catch up with network state.
    /// </summary>
    public class AnimationSpeedCompensator : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField] private float _maxSpeedMultiplier = 2.0f;
        [SerializeField] private float _compensationRate = 0.1f;
        [SerializeField] private float _normalSpeedThreshold = 0.05f;
        
        #endregion
        
        #region Dependencies
        
        private NetworkCharacterController _networkController;
        private MxMAnimator _mxmAnimator;
        private DualTrajectorySystem _trajectorySystem;
        
        #endregion
        
        private float _currentSpeedMultiplier = 1.0f;
        private uint _lastReconcileTick = 0;
        
        private void Update()
        {
            if (_networkController.IsOwner)
            {
                AdjustAnimationSpeed();
            }
        }
        
        private void AdjustAnimationSpeed()
        {
            // Calculate how far behind visual state is from network state
            var visualStateTicksBehind = CalculateVisualLag();
            
            if (visualStateTicksBehind > 2)
            {
                // Speed up animation to catch up
                var speedIncrease = 1.0f + (visualStateTicksBehind * _compensationRate);
                _currentSpeedMultiplier = Mathf.Min(_maxSpeedMultiplier, speedIncrease);
                
                _mxmAnimator.speed = _currentSpeedMultiplier;
            }
            else if (visualStateTicksBehind < _normalSpeedThreshold)
            {
                // Gradually return to normal speed
                _currentSpeedMultiplier = Mathf.Lerp(
                    _currentSpeedMultiplier, 
                    1.0f, 
                    Time.deltaTime * 2.0f);
                
                _mxmAnimator.speed = _currentSpeedMultiplier;
            }
        }
        
        private float CalculateVisualLag()
        {
            // Compare visual trajectory timestamp with network trajectory
            var networkTime = _trajectorySystem.GetNetworkTrajectoryTime();
            var visualTime = _trajectorySystem.GetVisualTrajectoryTime();
            
            return (networkTime - visualTime) / Time.fixedDeltaTime;
        }
    }
}
```

#### Strategy 2: Blend Masking for Partial Corrections
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Selectively blends different body parts during reconciliation.
    /// Allows upper body animations to continue while correcting lower body movement.
    /// </summary>
    public class ReconciliationBlendMasking : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField] private AvatarMask _upperBodyMask;
        [SerializeField] private AvatarMask _lowerBodyMask;
        [SerializeField] private float _blendDuration = 0.5f;
        
        #endregion
        
        #region Animation Layers
        
        private const int UpperBodyLayer = 1;
        private const int LowerBodyLayer = 2;
        private const int CorrectionLayer = 3;
        
        #endregion
        
        private bool _isReconciling = false;
        private float _reconcileStartTime;
        private float _originalUpperWeight;
        private float _originalLowerWeight;
        
        private void Start()
        {
            // Setup layer masks
            _mxmAnimator.SetLayerWeight(UpperBodyLayer, 1.0f);
            _mxmAnimator.SetLayerWeight(LowerBodyLayer, 1.0f);
            _mxmAnimator.SetLayerWeight(CorrectionLayer, 0.0f);
        }
        
        /// <summary>
        /// Begin reconciliation blending process.
        /// </summary>
        public void StartReconciliation(Vector3 positionError, float rotationError)
        {
            if (_isReconciling) return;
            
            _isReconciling = true;
            _reconcileStartTime = Time.time;
            
            // Store original weights
            _originalUpperWeight = _mxmAnimator.GetLayerWeight(UpperBodyLayer);
            _originalLowerWeight = _mxmAnimator.GetLayerWeight(LowerBodyLayer);
            
            // Determine blending strategy based on error magnitude
            if (positionError.magnitude > 0.5f)
            {
                // Large position error - blend entire body
                StartFullBodyCorrection();
            }
            else
            {
                // Small error - only blend lower body
                StartLowerBodyCorrection();
            }
        }
        
        private void StartLowerBodyCorrection()
        {
            // Reduce lower body influence, maintain upper body
            _mxmAnimator.SetLayerWeight(UpperBodyLayer, 1.0f);
            _mxmAnimator.SetLayerWeight(LowerBodyLayer, 0.3f);
            _mxmAnimator.SetLayerWeight(CorrectionLayer, 0.7f);
        }
        
        private void StartFullBodyCorrection()
        {
            // Blend everything during major corrections
            _mxmAnimator.SetLayerWeight(UpperBodyLayer, 0.5f);
            _mxmAnimator.SetLayerWeight(LowerBodyLayer, 0.3f);
            _mxmAnimator.SetLayerWeight(CorrectionLayer, 1.0f);
        }
        
        private void Update()
        {
            if (_isReconciling)
            {
                UpdateReconciliationBlend();
            }
        }
        
        private void UpdateReconciliationBlend()
        {
            var elapsed = Time.time - _reconcileStartTime;
            var blendFactor = Mathf.Clamp01(elapsed / _blendDuration);
            
            if (blendFactor >= 1.0f)
            {
                // Reconciliation complete, restore original weights
                _mxmAnimator.SetLayerWeight(UpperBodyLayer, _originalUpperWeight);
                _mxmAnimator.SetLayerWeight(LowerBodyLayer, _originalLowerWeight);
                _mxmAnimator.SetLayerWeight(CorrectionLayer, 0.0f);
                
                _isReconciling = false;
            }
            else
            {
                // Gradually return to normal blending
                var correctionWeight = Mathf.Lerp(1.0f, 0.0f, blendFactor);
                _mxmAnimator.SetLayerWeight(CorrectionLayer, correctionWeight);
            }
        }
    }
}
```

#### Strategy 3: Procedural Correction Layers
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Applies procedural adjustments on top of animation to reduce visual errors.
    /// Uses IK and bone manipulation to maintain visual quality during corrections.
    /// </summary>
    public class ProceduralCorrectionSystem : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField] private Transform _hipBone;
        [SerializeField] private Transform _leftFoot;
        [SerializeField] private Transform _rightFoot;
        [SerializeField] private float _correctionStrength = 0.1f;
        [SerializeField] private LayerMask _groundLayerMask = 1;
        
        #endregion
        
        #region State
        
        private Vector3 _positionError = Vector3.zero;
        private Vector3 _targetGroundPosition;
        private bool _hasPositionError = false;
        
        #endregion
        
        /// <summary>
        /// Apply position error that needs procedural correction.
        /// </summary>
        public void SetPositionError(Vector3 networkPosition, Vector3 visualPosition)
        {
            _positionError = networkPosition - visualPosition;
            _hasPositionError = _positionError.magnitude > 0.01f;
            
            if (_hasPositionError)
            {
                // Calculate target ground position for foot IK
                if (Physics.Raycast(networkPosition + Vector3.up, Vector3.down, 
                    out RaycastHit hit, 2.0f, _groundLayerMask))
                {
                    _targetGroundPosition = hit.point;
                }
            }
        }
        
        private void LateUpdate()
        {
            if (_hasPositionError)
            {
                ApplyProceduralCorrections();
            }
        }
        
        private void ApplyProceduralCorrections()
        {
            // Apply hip position correction
            var hipCorrection = _positionError * _correctionStrength;
            _hipBone.position += hipCorrection;
            
            // Adjust foot IK to maintain ground contact
            AdjustFootIK(hipCorrection);
            
            // Gradually reduce error
            _positionError *= (1.0f - _correctionStrength * Time.deltaTime);
            
            if (_positionError.magnitude < 0.001f)
            {
                _hasPositionError = false;
            }
        }
        
        private void AdjustFootIK(Vector3 correction)
        {
            // Left foot IK adjustment
            if (Physics.Raycast(_leftFoot.position + Vector3.up, Vector3.down,
                out RaycastHit leftHit, 2.0f, _groundLayerMask))
            {
                var leftFootTarget = leftHit.point;
                var leftCorrection = leftFootTarget - _leftFoot.position;
                
                // Apply gradual IK correction
                _leftFoot.position += leftCorrection * 0.5f;
            }
            
            // Right foot IK adjustment
            if (Physics.Raycast(_rightFoot.position + Vector3.up, Vector3.down,
                out RaycastHit rightHit, 2.0f, _groundLayerMask))
            {
                var rightFootTarget = rightHit.point;
                var rightCorrection = rightFootTarget - _rightFoot.position;
                
                // Apply gradual IK correction
                _rightFoot.position += rightCorrection * 0.5f;
            }
        }
    }
}
```

### Prediction Confidence System

#### Dynamic Animation Commitment
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Calculates prediction confidence and adjusts animation commitment accordingly.
    /// Reduces visual artifacts by avoiding complex animations during uncertain predictions.
    /// </summary>
    public class PredictionConfidenceManager : MonoBehaviour
    {
        #region Configuration
        
        [SerializeField] private float _lowConfidenceThreshold = 0.7f;
        [SerializeField] private int _inputStabilityFrames = 5;
        [SerializeField] private float _networkLatencyWeight = 0.3f;
        
        #endregion
        
        #region State Tracking
        
        private readonly Queue<Vector2> _recentInputs = new();
        private float _currentConfidence = 1.0f;
        private uint _lastValidatedTick = 0;
        
        #endregion
        
        /// <summary>
        /// Calculate prediction confidence based on multiple factors.
        /// </summary>
        public float CalculatePredictionConfidence(
            Vector2 currentInput,
            uint currentTick,
            float networkLatency)
        {
            // Factor 1: Input stability
            var inputStability = CalculateInputStability(currentInput);
            
            // Factor 2: Prediction distance (how far ahead we're predicting)
            var ticksAhead = currentTick - _lastValidatedTick;
            var predictionDistance = 1.0f - Mathf.Clamp01(ticksAhead / 10.0f);
            
            // Factor 3: Network latency impact
            var latencyFactor = 1.0f - Mathf.Clamp01(networkLatency * _networkLatencyWeight);
            
            // Combined confidence score
            _currentConfidence = (inputStability + predictionDistance + latencyFactor) / 3.0f;
            
            return _currentConfidence;
        }
        
        private float CalculateInputStability(Vector2 currentInput)
        {
            _recentInputs.Enqueue(currentInput);
            
            if (_recentInputs.Count > _inputStabilityFrames)
            {
                _recentInputs.Dequeue();
            }
            
            if (_recentInputs.Count < 2)
                return 1.0f;
            
            // Calculate variance in recent inputs
            var inputs = _recentInputs.ToArray();
            var variance = 0f;
            
            for (int i = 1; i < inputs.Length; i++)
            {
                variance += Vector2.Distance(inputs[i], inputs[i-1]);
            }
            
            variance /= inputs.Length - 1;
            
            // Higher variance = lower stability
            return 1.0f - Mathf.Clamp01(variance);
        }
        
        /// <summary>
        /// Check if current confidence allows for complex animations.
        /// </summary>
        public bool ShouldAllowComplexAnimations()
        {
            return _currentConfidence > _lowConfidenceThreshold;
        }
        
        /// <summary>
        /// Get animation commitment level based on confidence.
        /// </summary>
        public AnimationCommitmentLevel GetCommitmentLevel()
        {
            if (_currentConfidence > 0.8f)
                return AnimationCommitmentLevel.Full;
            else if (_currentConfidence > 0.6f)
                return AnimationCommitmentLevel.Moderate;
            else
                return AnimationCommitmentLevel.Conservative;
        }
        
        /// <summary>
        /// Update last validated tick from network reconciliation.
        /// </summary>
        public void OnNetworkReconciliation(uint validatedTick)
        {
            _lastValidatedTick = validatedTick;
        }
    }
    
    public enum AnimationCommitmentLevel
    {
        Conservative,  // Short, easily interruptible animations
        Moderate,      // Medium-length animations with some commitment
        Full           // Full animation commitment, including root motion
    }
}
```

### Acceptable vs Unacceptable Visual Artifacts

#### Artifact Classification System
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Classifies and manages visual artifacts during network reconciliation.
    /// Defines thresholds for acceptable visual quality degradation.
    /// </summary>
    public class VisualArtifactManager : MonoBehaviour
    {
        #region Artifact Thresholds
        
        [Header("Acceptable Artifacts")]
        [SerializeField] private float _acceptableFootSlide = 0.1f;           // 10cm foot sliding
        [SerializeField] private float _acceptableAnimationHitch = 0.1f;      // 100ms animation speed change
        [SerializeField] private float _acceptableIKAdjustment = 0.05f;       // 5cm IK adjustment
        [SerializeField] private float _acceptablePositionError = 0.2f;       // 20cm position error
        
        [Header("Unacceptable Artifacts")]
        [SerializeField] private float _maxTeleportDistance = 1.0f;           // 1m instant movement
        [SerializeField] private float _maxAnimationDesync = 2.0f;            // 2 second animation mismatch
        [SerializeField] private float _maxVisualEffectDelay = 0.5f;          // 500ms effect delay
        
        #endregion
        
        /// <summary>
        /// Evaluate if visual correction will cause acceptable artifacts.
        /// </summary>
        public CorrectionStrategy EvaluateCorrection(
            Vector3 positionError,
            float animationTimeError,
            float effectDelayError)
        {
            var strategy = new CorrectionStrategy();
            
            // Position correction evaluation
            if (positionError.magnitude > _maxTeleportDistance)
            {
                strategy.PositionCorrectionType = PositionCorrectionType.Gradual;
                strategy.CorrectionDuration = 1.0f;
            }
            else if (positionError.magnitude > _acceptablePositionError)
            {
                strategy.PositionCorrectionType = PositionCorrectionType.Smooth;
                strategy.CorrectionDuration = 0.5f;
            }
            else
            {
                strategy.PositionCorrectionType = PositionCorrectionType.Immediate;
                strategy.CorrectionDuration = 0.1f;
            }
            
            // Animation correction evaluation
            if (animationTimeError > _maxAnimationDesync)
            {
                strategy.AnimationCorrectionType = AnimationCorrectionType.Restart;
            }
            else if (animationTimeError > _acceptableAnimationHitch)
            {
                strategy.AnimationCorrectionType = AnimationCorrectionType.SpeedAdjust;
            }
            else
            {
                strategy.AnimationCorrectionType = AnimationCorrectionType.Continue;
            }
            
            // Effect correction evaluation
            if (effectDelayError > _maxVisualEffectDelay)
            {
                strategy.EffectCorrectionType = EffectCorrectionType.Cancel;
            }
            else
            {
                strategy.EffectCorrectionType = EffectCorrectionType.Delay;
            }
            
            return strategy;
        }
    }
    
    /// <summary>
    /// Correction strategy based on artifact evaluation.
    /// </summary>
    public struct CorrectionStrategy
    {
        public PositionCorrectionType PositionCorrectionType;
        public AnimationCorrectionType AnimationCorrectionType;
        public EffectCorrectionType EffectCorrectionType;
        public float CorrectionDuration;
    }
    
    public enum PositionCorrectionType
    {
        Immediate,  // Snap instantly (small errors)
        Smooth,     // Lerp over time (medium errors)
        Gradual     // Slow correction (large errors)
    }
    
    public enum AnimationCorrectionType
    {
        Continue,     // Keep current animation
        SpeedAdjust,  // Adjust playback speed
        Restart       // Restart from correct state
    }
    
    public enum EffectCorrectionType
    {
        Continue,     // Play effect as scheduled
        Delay,        // Delay effect to match network
        Cancel        // Cancel effect entirely
    }
}
```

### Performance Impact Monitoring

#### Dual Trajectory Performance Tracker
```csharp
namespace AhBearStudios.Unity.CharacterController.Animation
{
    /// <summary>
    /// Monitors performance impact of dual trajectory system and lag compensation.
    /// Provides metrics for optimization and quality adjustment.
    /// </summary>
    public class DualTrajectoryPerformanceTracker : MonoBehaviour
    {
        #region Dependencies
        
        [Inject] private IProfilerService _profilerService;
        [Inject] private ILoggingService _logger;
        
        #endregion
        
        #region Performance Metrics
        
        private readonly ProfilerMarker _trajectoryUpdateMarker = 
            new("DualTrajectory.Update");
        private readonly ProfilerMarker _animationCorrectionMarker = 
            new("DualTrajectory.AnimationCorrection");
        private readonly ProfilerMarker _checkpointMarker = 
            new("DualTrajectory.Checkpoint");
        
        private float _lastTrajectoryUpdateTime;
        private int _correctionCount = 0;
        private int _checkpointCount = 0;
        private float _totalCorrectionTime = 0f;
        
        #endregion
        
        /// <summary>
        /// Record trajectory update performance.
        /// </summary>
        public void RecordTrajectoryUpdate(float deltaTime)
        {
            using (_trajectoryUpdateMarker.Auto())
            {
                _lastTrajectoryUpdateTime = deltaTime;
            }
        }
        
        /// <summary>
        /// Record animation correction performance.
        /// </summary>
        public void RecordAnimationCorrection(float correctionTime)
        {
            using (_animationCorrectionMarker.Auto())
            {
                _correctionCount++;
                _totalCorrectionTime += correctionTime;
            }
        }
        
        /// <summary>
        /// Record checkpoint creation performance.
        /// </summary>
        public void RecordCheckpointCreation()
        {
            using (_checkpointMarker.Auto())
            {
                _checkpointCount++;
            }
        }
        
        /// <summary>
        /// Get current performance metrics.
        /// </summary>
        public DualTrajectoryMetrics GetMetrics()
        {
            return new DualTrajectoryMetrics
            {
                LastTrajectoryUpdateTime = _lastTrajectoryUpdateTime,
                CorrectionsPerSecond = _correctionCount / Time.time,
                AverageCorrectionTime = _correctionCount > 0 ? _totalCorrectionTime / _correctionCount : 0f,
                CheckpointsCreated = _checkpointCount,
                MemoryUsage = _profilerService.GetMemoryUsage("DualTrajectory")
            };
        }
        
        /// <summary>
        /// Log performance warning if thresholds exceeded.
        /// </summary>
        private void Update()
        {
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                var metrics = GetMetrics();
                
                if (metrics.LastTrajectoryUpdateTime > 2.0f) // 2ms threshold
                {
                    _logger.LogWarning($"Dual trajectory update taking {metrics.LastTrajectoryUpdateTime:F2}ms");
                }
                
                if (metrics.CorrectionsPerSecond > 5.0f)
                {
                    _logger.LogWarning($"High correction frequency: {metrics.CorrectionsPerSecond:F1}/sec");
                }
            }
        }
    }
    
    /// <summary>
    /// Performance metrics for dual trajectory system.
    /// </summary>
    public struct DualTrajectoryMetrics
    {
        public float LastTrajectoryUpdateTime;
        public float CorrectionsPerSecond;
        public float AverageCorrectionTime;
        public int CheckpointsCreated;
        public long MemoryUsage;
    }
}
```

---

## Performance Architecture (Jobs & Burst)

The character controller leverages Unity's Jobs System and Burst compiler for high-performance, parallelizable movement processing. The architecture supports multiple characters processing simultaneously while maintaining deterministic behavior for networking.

### Job System Architecture

#### Movement Processing Job
```csharp
namespace AhBearStudios.Core.CharacterController.Jobs
{
    [BurstCompile]
    public struct CharacterMovementJob : IJobParallelFor
    {
        [ReadOnly] 
        public NativeArray<MovementContext> contexts;
        
        [ReadOnly]
        public NativeArray<CharacterReplicateData> inputData;
        
        [WriteOnly]
        public NativeArray<MovementResult> results;
        
        [ReadOnly]
        public float deltaTime;
        
        [ReadOnly]
        public CollisionWorld collisionWorld;
        
        public void Execute(int index)
        {
            var context = contexts[index];
            var input = inputData[index];
            
            // Execute HTN movement tasks
            var result = ProcessMovement(context, input, deltaTime, collisionWorld);
            results[index] = result;
        }
        
        private MovementResult ProcessMovement(
            MovementContext context,
            CharacterReplicateData input,
            float deltaTime,
            CollisionWorld collisionWorld)
        {
            // Burst-compatible movement processing
            var velocity = context.Velocity;
            var position = context.Position;
            
            // Apply input forces
            velocity += CalculateInputForces(input, context) * deltaTime;
            
            // Apply gravity and environmental forces
            velocity += CalculateEnvironmentalForces(context) * deltaTime;
            
            // Perform collision detection and resolution
            var collisionInfo = PerformCollisionDetection(
                position, velocity, deltaTime, collisionWorld);
            
            return new MovementResult
            {
                Position = collisionInfo.Position,
                Velocity = collisionInfo.Velocity,
                IsGrounded = collisionInfo.IsGrounded,
                GroundNormal = collisionInfo.GroundNormal
            };
        }
    }
}
```

#### Collision Detection Job
```csharp
namespace AhBearStudios.Core.CharacterController.Jobs
{
    [BurstCompile]
    public struct CharacterCollisionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float3> positions;
        
        [ReadOnly]
        public NativeArray<float3> velocities;
        
        [ReadOnly]
        public CollisionWorld collisionWorld;
        
        [WriteOnly]
        public NativeArray<CollisionInfo> collisionResults;
        
        [ReadOnly]
        public CharacterCollisionSettings settings;
        
        [ReadOnly]
        public float deltaTime;
        
        public void Execute(int index)
        {
            var position = positions[index];
            var velocity = velocities[index];
            
            // Perform swept capsule collision
            var collisionInfo = PerformSweptCapsuleCollision(
                position, velocity, deltaTime, settings, collisionWorld);
            
            collisionResults[index] = collisionInfo;
        }
    }
}
```

### Spatial Partitioning for Collision Optimization

#### Spatial Hash System
```csharp
namespace AhBearStudios.Core.CharacterController.Spatial
{
    public struct SpatialHashGrid
    {
        private NativeMultiHashMap<int, int> _grid;
        private float _cellSize;
        private int _gridWidth;
        private int _gridHeight;
        
        public void Insert(int characterIndex, float3 position)
        {
            var cellIndex = GetCellIndex(position);
            _grid.Add(cellIndex, characterIndex);
        }
        
        public NativeArray<int> GetNearbyCharacters(
            float3 position, 
            float radius,
            Allocator allocator)
        {
            // Query nearby cells for potential collisions
            var results = new NativeList<int>(allocator);
            
            var minCell = GetCellIndex(position - radius);
            var maxCell = GetCellIndex(position + radius);
            
            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int z = minCell.z; z <= maxCell.z; z++)
                {
                    var cellIndex = GetCellIndex(new float3(x, 0, z));
                    if (_grid.TryGetFirstValue(cellIndex, out int item, out var iterator))
                    {
                        do
                        {
                            results.Add(item);
                        } 
                        while (_grid.TryGetNextValue(out item, ref iterator));
                    }
                }
            }
            
            return results.ToArray(allocator);
        }
    }
}
```

### Memory Layout Optimization

#### Structure of Arrays (SoA) Pattern
```csharp
namespace AhBearStudios.Core.CharacterController.Data
{
    /// <summary>
    /// Optimized data layout for batch processing multiple characters.
    /// Uses Structure of Arrays for better cache locality and SIMD processing.
    /// </summary>
    public struct CharacterBatchData : IDisposable
    {
        // Position data
        public NativeArray<float3> Positions;
        public NativeArray<float3> Velocities;
        public NativeArray<quaternion> Rotations;
        
        // State data
        public NativeArray<bool> IsGrounded;
        public NativeArray<float3> GroundNormals;
        public NativeArray<float> StaminaValues;
        public NativeArray<float> SpeedMultipliers;
        
        // Input data
        public NativeArray<float2> MovementInputs;
        public NativeArray<bool> JumpInputs;
        public NativeArray<bool> RunInputs;
        
        // Configuration indices
        public NativeArray<int> ConfigurationIndices;
        
        public int Count { get; private set; }
        
        public CharacterBatchData(int capacity, Allocator allocator)
        {
            Positions = new NativeArray<float3>(capacity, allocator);
            Velocities = new NativeArray<float3>(capacity, allocator);
            Rotations = new NativeArray<quaternion>(capacity, allocator);
            IsGrounded = new NativeArray<bool>(capacity, allocator);
            GroundNormals = new NativeArray<float3>(capacity, allocator);
            StaminaValues = new NativeArray<float>(capacity, allocator);
            SpeedMultipliers = new NativeArray<float>(capacity, allocator);
            MovementInputs = new NativeArray<float2>(capacity, allocator);
            JumpInputs = new NativeArray<bool>(capacity, allocator);
            RunInputs = new NativeArray<bool>(capacity, allocator);
            ConfigurationIndices = new NativeArray<int>(capacity, allocator);
            
            Count = 0;
        }
        
        public void Dispose()
        {
            if (Positions.IsCreated) Positions.Dispose();
            if (Velocities.IsCreated) Velocities.Dispose();
            if (Rotations.IsCreated) Rotations.Dispose();
            if (IsGrounded.IsCreated) IsGrounded.Dispose();
            if (GroundNormals.IsCreated) GroundNormals.Dispose();
            if (StaminaValues.IsCreated) StaminaValues.Dispose();
            if (SpeedMultipliers.IsCreated) SpeedMultipliers.Dispose();
            if (MovementInputs.IsCreated) MovementInputs.Dispose();
            if (JumpInputs.IsCreated) JumpInputs.Dispose();
            if (RunInputs.IsCreated) RunInputs.Dispose();
            if (ConfigurationIndices.IsCreated) ConfigurationIndices.Dispose();
        }
    }
}
```

### Performance Manager Integration

#### Character Performance Service
```csharp
namespace AhBearStudios.Core.CharacterController.Services
{
    public class CharacterPerformanceService : ICharacterPerformanceService, IDisposable
    {
        private readonly IProfilerService _profilerService;
        private readonly ILoggingService _loggingService;
        private readonly ProfilerMarker _movementJobMarker;
        private readonly ProfilerMarker _collisionJobMarker;
        private readonly ProfilerMarker _batchUpdateMarker;
        
        private CharacterBatchData _batchData;
        private JobHandle _currentJobHandle;
        private readonly List<NetworkCharacterController> _characters;
        
        public CharacterPerformanceService(
            IProfilerService profilerService,
            ILoggingService loggingService)
        {
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            _movementJobMarker = new ProfilerMarker("CharacterController.MovementJob");
            _collisionJobMarker = new ProfilerMarker("CharacterController.CollisionJob");
            _batchUpdateMarker = new ProfilerMarker("CharacterController.BatchUpdate");
            
            _characters = new List<NetworkCharacterController>();
            _batchData = new CharacterBatchData(1000, Allocator.Persistent);
        }
        
        public void RegisterCharacter(NetworkCharacterController character)
        {
            _characters.Add(character);
            _loggingService.LogDebug($"Registered character for batch processing: {character.name}");
        }
        
        public void UnregisterCharacter(NetworkCharacterController character)
        {
            _characters.Remove(character);
            _loggingService.LogDebug($"Unregistered character from batch processing: {character.name}");
        }
        
        public JobHandle ScheduleBatchUpdate(float deltaTime)
        {
            using (_batchUpdateMarker.Auto())
            {
                // Complete previous frame
                _currentJobHandle.Complete();
                
                // Prepare batch data
                PrepareBatchData();
                
                // Schedule movement job
                var movementJob = new CharacterMovementJob
                {
                    contexts = _batchData.GetContextArray(),
                    inputData = _batchData.GetInputArray(),
                    results = _batchData.GetResultArray(),
                    deltaTime = deltaTime,
                    collisionWorld = GetCollisionWorld()
                };
                
                _currentJobHandle = movementJob.Schedule(
                    _characters.Count, 
                    32, // Inner loop batch size
                    _currentJobHandle);
                
                return _currentJobHandle;
            }
        }
        
        public void CompleteJobs()
        {
            _currentJobHandle.Complete();
            ApplyResults();
        }
        
        public CharacterPerformanceMetrics GetPerformanceMetrics()
        {
            return new CharacterPerformanceMetrics
            {
                ActiveCharacterCount = _characters.Count,
                LastFrameTime = _profilerService.GetLastFrameTime("CharacterController"),
                AverageFrameTime = _profilerService.GetAverageFrameTime("CharacterController", 60),
                MemoryUsage = _profilerService.GetMemoryUsage("CharacterController")
            };
        }
        
        public void Dispose()
        {
            _currentJobHandle.Complete();
            _batchData.Dispose();
        }
    }
}
```

### Burst Compilation Settings

#### Assembly Definition Configuration
```json
{
    "name": "AhBearStudios.Core.CharacterController",
    "rootNamespace": "AhBearStudios.Core.CharacterController",
    "references": [
        "Unity.Collections",
        "Unity.Jobs",
        "Unity.Burst",
        "Unity.Mathematics",
        "Unity.Physics",
        "AhBearStudios.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

#### Burst Compilation Attributes
```csharp
// All movement jobs use aggressive Burst optimization
[BurstCompile(
    CompileSynchronously = true,
    FloatMode = FloatMode.Fast,
    FloatPrecision = FloatPrecision.Standard)]
public struct OptimizedMovementJob : IJobParallelFor
{
    // Implementation with focus on SIMD optimization
}
```

---

## Implementation Roadmap

The character controller refactoring follows a phased approach to minimize disruption while maximizing development velocity and testing opportunities.

### Phase 1: Core HTN Infrastructure (Weeks 1-3)

**Week 1: HTN Foundation**
- [ ] Create HTN base interfaces (ICondition, IEffect, IOperator, ITask)
- [ ] Implement MovementContext with AhBearStudios.Core integration
- [ ] Build HTN planner for simple movement tasks
- [ ] Create basic movement tasks (Walk, Jump, Idle)
- [ ] Unit tests for HTN system

**Week 2: FishNet Integration Foundation**
- [ ] Implement IReplicateData/IReconcileData structures
- [ ] Create CharacterReplicateData with compression support
- [ ] Build basic NetworkCharacterController with prediction
- [ ] Implement tick-based movement synchronization
- [ ] Network tests with 2-4 clients

**Week 3: Builder → Config → Factory → Service Pattern**
- [ ] CharacterControllerConfigBuilder with fluent API
- [ ] CharacterControllerConfig with validation
- [ ] CharacterControllerServiceFactory
- [ ] CharacterControllerService with core movement
- [ ] Integration tests with AhBearStudios services

### Phase 2: Advanced HTN Movement (Weeks 4-6)

**Week 4: Complex Movement Tasks**
- [ ] Swimming task with underwater physics
- [ ] Climbing task with surface detection
- [ ] Crouching task with collision adjustment
- [ ] Rope swinging with joint physics
- [ ] Task transition system with blending

**Week 5: Environmental Interaction**
- [ ] Platform attachment/detachment tasks
- [ ] Moving platform synchronization
- [ ] Environmental hazard detection
- [ ] Slope and stair navigation
- [ ] Interactive object tasks (doors, levers, etc.)

**Week 6: Combat Integration**
- [ ] Attack task framework
- [ ] Dodge and block tasks
- [ ] Damage-based movement modifications
- [ ] Knockback and stun effects
- [ ] Weapon-specific movement constraints

### Phase 3: Performance & Jobs System (Weeks 7-9)

**Week 7: Unity Jobs Integration**
- [ ] CharacterMovementJob with Burst compilation
- [ ] CharacterCollisionJob for parallel collision detection
- [ ] Spatial partitioning system for optimization
- [ ] Structure of Arrays (SoA) data layout
- [ ] Performance profiling integration

**Week 8: Batch Processing**
- [ ] CharacterBatchData for multiple character processing
- [ ] CharacterPerformanceService for job scheduling
- [ ] Memory pool integration for zero allocation
- [ ] SIMD optimization for movement calculations
- [ ] Performance benchmarks and validation

**Week 9: Collision Optimization**
- [ ] Spatial hash grid for collision queries
- [ ] Broadphase collision detection
- [ ] Swept capsule collision with Burst
- [ ] Multi-threaded collision resolution
- [ ] Performance testing with 100+ characters

### Phase 4: Motion Matching Integration (Weeks 10-12)

**Week 10: MxM Foundation**
- [ ] DualTrajectorySystem implementation
- [ ] Network trajectory vs visual trajectory separation
- [ ] TrajectoryPoint serialization with compression
- [ ] MxM animator integration points
- [ ] Animation event synchronization

**Week 11: Advanced Animation Features**
- [ ] AnimationEventSynchronizer with network timing
- [ ] RootMotionHandler with multiple modes
- [ ] Animation rollback for prediction errors
- [ ] IK integration for foot placement
- [ ] Animation compression for bandwidth optimization

**Week 12: Animation-Movement Coordination**
- [ ] HTN task → MxM animation coordination
- [ ] Animation-driven movement constraints
- [ ] Transition blending between movement types
- [ ] Animation event → HTN task triggers
- [ ] Performance optimization for animation system

### Phase 5: Testing & Polish (Weeks 13-15)

**Week 13: Integration Testing**
- [ ] Multi-client stress testing (20+ players)
- [ ] Network prediction accuracy validation
- [ ] Performance benchmarking on target hardware
- [ ] Memory leak detection and profiling
- [ ] Cross-platform compatibility testing

**Week 14: Edge Case Handling**
- [ ] Network packet loss scenarios
- [ ] High latency compensation
- [ ] Frame rate drops and stuttering
- [ ] Platform-specific input handling
- [ ] Accessibility features

**Week 15: Documentation & Deployment**
- [ ] API documentation generation
- [ ] Movement task creation tutorials
- [ ] Performance optimization guide
- [ ] Debugging and profiling documentation
- [ ] Migration guide from old system

### Phase 6: Advanced Features (Weeks 16+)

**Optional Advanced Features:**
- [ ] AI agent integration with HTN system
- [ ] Procedural animation blending
- [ ] Advanced physics integration (vehicles, flying)
- [ ] VR/AR specific movement adaptations
- [ ] Advanced networking features (lag compensation)

### Success Criteria

**Performance Targets:**
- 60+ FPS with 100+ networked characters
- <5ms movement processing time per frame
- <1MB memory allocation per character
- <10ms network prediction error correction
- 99.9% uptime under normal load

**Quality Targets:**
- Zero breaking changes to existing character prefabs
- Full unit test coverage (>90%) for HTN system
- Integration test coverage for all movement types
- Performance regression detection in CI/CD
- Code review approval for all changes

**Delivery Targets:**
- Weekly sprint reviews with stakeholders
- Bi-weekly performance benchmarking
- Monthly integration testing with full game
- Quarterly platform compatibility validation
- Continuous deployment to development environment

---
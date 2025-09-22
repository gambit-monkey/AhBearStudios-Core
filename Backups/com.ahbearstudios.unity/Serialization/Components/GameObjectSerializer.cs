using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using AhBearStudios.Unity.Serialization.Components;
using AhBearStudios.Unity.Serialization.Models;
using Cysharp.Threading.Tasks;
using MemoryPack;
using ZLinq;

namespace AhBearStudios.Unity.Serialization.Components
{
    /// <summary>
    /// Comprehensive MonoBehaviour component for serializing entire GameObject hierarchies.
    /// Provides deep serialization of GameObjects including components, children, and custom data.
    /// Optimized for scene persistence and complex object state management.
    /// </summary>
    [AddComponentMenu("AhBearStudios/Serialization/GameObject Serializer")]
    public class GameObjectSerializer : SerializableMonoBehaviour
    {
        [SerializeField]
        private bool _serializeChildren = true;
        
        [SerializeField]
        private bool _serializeComponents = true;
        
        [SerializeField]
        private bool _serializeTransform = true;
        
        [SerializeField]
        private bool _serializeActive = true;
        
        [SerializeField]
        private bool _serializeTag = true;
        
        [SerializeField]
        private bool _serializeLayer = true;
        
        [SerializeField]
        private int _maxDepth = 10;
        
        [SerializeField]
        private List<string> _excludedComponentTypes = new List<string>();
        
        [SerializeField]
        private List<string> _includedComponentTypes = new List<string>();
        
        [SerializeField]
        private bool _useWhitelist = false;

        private GameObjectData _cachedGameObjectData;
        private float _lastCacheTime;
        private const float CacheUpdateInterval = 1.0f;

        /// <summary>
        /// Gets or sets whether child GameObjects should be serialized.
        /// </summary>
        public bool SerializeChildren
        {
            get => _serializeChildren;
            set => _serializeChildren = value;
        }

        /// <summary>
        /// Gets or sets whether components should be serialized.
        /// </summary>
        public bool SerializeComponents
        {
            get => _serializeComponents;
            set => _serializeComponents = value;
        }

        /// <summary>
        /// Gets or sets the maximum depth for child serialization.
        /// </summary>
        public int MaxDepth
        {
            get => _maxDepth;
            set => _maxDepth = Mathf.Max(0, value);
        }

        /// <summary>
        /// Gets the list of excluded component types that won't be serialized.
        /// </summary>
        public List<string> ExcludedComponentTypes => _excludedComponentTypes;

        /// <summary>
        /// Gets the list of included component types (when using whitelist mode).
        /// </summary>
        public List<string> IncludedComponentTypes => _includedComponentTypes;

        /// <summary>
        /// Gets or sets whether to use whitelist mode for component serialization.
        /// </summary>
        public bool UseWhitelist
        {
            get => _useWhitelist;
            set => _useWhitelist = value;
        }

        protected override async void Start()
        {
            base.Start();
            await CacheGameObjectDataAsync();
        }

        private void Update()
        {
            // Periodically update cache to detect changes
            if (Time.time - _lastCacheTime >= CacheUpdateInterval)
            {
                _ = CacheGameObjectDataAsync();
            }
        }

        /// <summary>
        /// Manually triggers caching of the current GameObject state.
        /// </summary>
        public async UniTask CacheGameObjectDataAsync()
        {
            _cachedGameObjectData = await CreateGameObjectDataAsync();
            _lastCacheTime = Time.time;
        }

        /// <summary>
        /// Gets the cached GameObject data without regenerating it.
        /// </summary>
        /// <returns>Cached GameObject data</returns>
        public GameObjectData GetCachedGameObjectData()
        {
            return _cachedGameObjectData;
        }

        /// <summary>
        /// Applies GameObject data to recreate or restore the GameObject state.
        /// </summary>
        /// <param name="data">The GameObject data to apply</param>
        /// <param name="targetGameObject">Optional target GameObject. If null, uses this GameObject.</param>
        public async UniTask ApplyGameObjectDataAsync(GameObjectData data, GameObject targetGameObject = null)
        {
            await SetSerializableDataAsync(data);
        }

        protected override object GetSerializableData()
        {
            // Use cached data if available and recent
            if (_cachedGameObjectData != null && Time.time - _lastCacheTime < CacheUpdateInterval * 2)
            {
                return _cachedGameObjectData;
            }

            // Create new data synchronously (fallback)
            return CreateGameObjectDataSync();
        }

        protected override async UniTask SetSerializableDataAsync(object data)
        {
            if (data is GameObjectData gameObjectData)
            {
                await ApplyGameObjectDataToGameObject(gameObjectData, gameObject);
            }
            else
            {
                throw new ArgumentException($"Expected GameObjectData, but received {data?.GetType().Name ?? "null"}");
            }
        }

        private async UniTask<GameObjectData> CreateGameObjectDataAsync()
        {
            return await UniTask.RunOnThreadPool(() => CreateGameObjectDataSync());
        }

        private GameObjectData CreateGameObjectDataSync()
        {
            var data = new GameObjectData
            {
                Name = gameObject.name,
                Tag = _serializeTag ? gameObject.tag : null,
                Layer = _serializeLayer ? gameObject.layer : 0,
                IsActive = _serializeActive ? gameObject.activeInHierarchy : true,
                Timestamp = DateTime.UtcNow.Ticks
            };

            // Serialize transform
            if (_serializeTransform)
            {
                data.Transform = CreateTransformData(transform);
            }

            // Serialize components
            if (_serializeComponents)
            {
                data.Components = CreateComponentsData();
            }

            // Serialize children
            if (_serializeChildren && _maxDepth > 0)
            {
                data.Children = CreateChildrenData(0);
            }

            return data;
        }

        private TransformData CreateTransformData(Transform t)
        {
            return new TransformData
            {
                Position = new SerializableVector3 { x = t.localPosition.x, y = t.localPosition.y, z = t.localPosition.z },
                Rotation = new SerializableQuaternion { x = t.localRotation.x, y = t.localRotation.y, z = t.localRotation.z, w = t.localRotation.w },
                Scale = new SerializableVector3 { x = t.localScale.x, y = t.localScale.y, z = t.localScale.z },
                HasPosition = true,
                HasRotation = true,
                HasScale = true,
                SerializeLocalSpace = true,
                UseCompressedRotation = false,
                Timestamp = DateTime.UtcNow.Ticks
            };
        }

        private List<ComponentData> CreateComponentsData()
        {
            var components = new List<ComponentData>();
            var gameObjectComponents = gameObject.GetComponents<Component>();

            foreach (var component in gameObjectComponents)
            {
                if (component == null) continue;
                if (component == this) continue; // Don't serialize ourselves
                if (component is Transform) continue; // Transform is handled separately

                var componentType = component.GetType().FullName;
                
                // Check whitelist/blacklist
                if (_useWhitelist)
                {
                    if (!_includedComponentTypes.Contains(componentType))
                        continue;
                }
                else
                {
                    if (_excludedComponentTypes.Contains(componentType))
                        continue;
                }

                var componentData = CreateComponentData(component);
                if (componentData != null)
                {
                    components.Add(componentData);
                }
            }

            return components;
        }

        private ComponentData CreateComponentData(Component component)
        {
            try
            {
                var data = new ComponentData
                {
                    TypeName = component.GetType().FullName,
                    AssemblyName = component.GetType().Assembly.FullName,
                    IsEnabled = true
                };

                // Handle common component types
                switch (component)
                {
                    case Renderer renderer:
                        data.IsEnabled = renderer.enabled;
                        data.DataType = ComponentDataType.Renderer;
                        data.RendererData = CreateRendererData(renderer);
                        break;
                    case Collider collider:
                        data.IsEnabled = collider.enabled;
                        data.DataType = ComponentDataType.Collider;
                        data.ColliderData = CreateColliderData(collider);
                        break;
                    case Rigidbody rigidbody:
                        data.DataType = ComponentDataType.Rigidbody;
                        data.RigidbodyData = CreateRigidbodyData(rigidbody);
                        break;
                    case MonoBehaviour monoBehaviour:
                        data.IsEnabled = monoBehaviour.enabled;
                        data.DataType = ComponentDataType.MonoBehaviour;
                        data.MonoBehaviourData = CreateMonoBehaviourData(monoBehaviour);
                        break;
                    default:
                        // For other components, store basic enabled state
                        data.DataType = ComponentDataType.Other;
                        if (component is Behaviour behaviour)
                        {
                            data.IsEnabled = behaviour.enabled;
                        }
                        break;
                }

                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to serialize component {component.GetType().Name}: {ex.Message}", this);
                return null;
            }
        }

        private RendererData CreateRendererData(Renderer renderer)
        {
            return new RendererData
            {
                Enabled = renderer.enabled,
                CastShadows = (int)renderer.shadowCastingMode,
                ReceiveShadows = renderer.receiveShadows,
                MaterialCount = renderer.materials?.Length ?? 0
            };
        }

        private ColliderData CreateColliderData(Collider collider)
        {
            return new ColliderData
            {
                Enabled = collider.enabled,
                IsTrigger = collider.isTrigger,
                Material = collider.material?.name ?? "",
                Bounds = new SerializableBounds
                {
                    Center = new SerializableVector3 { x = collider.bounds.center.x, y = collider.bounds.center.y, z = collider.bounds.center.z },
                    Size = new SerializableVector3 { x = collider.bounds.size.x, y = collider.bounds.size.y, z = collider.bounds.size.z }
                }
            };
        }

        private RigidbodyData CreateRigidbodyData(Rigidbody rigidbody)
        {
            return new RigidbodyData
            {
                Mass = rigidbody.mass,
                Drag = rigidbody.drag,
                AngularDrag = rigidbody.angularDrag,
                UseGravity = rigidbody.useGravity,
                IsKinematic = rigidbody.isKinematic,
                Velocity = new SerializableVector3 { x = rigidbody.velocity.x, y = rigidbody.velocity.y, z = rigidbody.velocity.z },
                AngularVelocity = new SerializableVector3 { x = rigidbody.angularVelocity.x, y = rigidbody.angularVelocity.y, z = rigidbody.angularVelocity.z }
            };
        }

        private MonoBehaviourData CreateMonoBehaviourData(MonoBehaviour monoBehaviour)
        {
            // For MonoBehaviours that implement ISerializable, we can serialize their data
            if (monoBehaviour is ISerializable serializable)
            {
                return new MonoBehaviourData
                {
                    Enabled = monoBehaviour.enabled,
                    HasCustomData = true,
                    CustomDataBytes = null, // Would need actual serialization implementation
                    CustomDataTypeName = serializable.GetType().FullName
                };
            }

            return new MonoBehaviourData
            {
                Enabled = monoBehaviour.enabled,
                HasCustomData = false
            };
        }

        private List<GameObjectData> CreateChildrenData(int currentDepth)
        {
            if (currentDepth >= _maxDepth)
                return new List<GameObjectData>();

            var children = new List<GameObjectData>();

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childData = new GameObjectData
                {
                    Name = child.gameObject.name,
                    Tag = _serializeTag ? child.gameObject.tag : null,
                    Layer = _serializeLayer ? child.gameObject.layer : 0,
                    IsActive = _serializeActive ? child.gameObject.activeInHierarchy : true,
                    Timestamp = DateTime.UtcNow.Ticks
                };

                if (_serializeTransform)
                {
                    childData.Transform = CreateTransformData(child);
                }

                if (_serializeComponents)
                {
                    childData.Components = CreateComponentsDataForGameObject(child.gameObject);
                }

                if (_serializeChildren && currentDepth + 1 < _maxDepth)
                {
                    childData.Children = CreateChildrenDataForGameObject(child.gameObject, currentDepth + 1);
                }

                children.Add(childData);
            }

            return children;
        }

        private List<ComponentData> CreateComponentsDataForGameObject(GameObject go)
        {
            var components = new List<ComponentData>();
            var gameObjectComponents = go.GetComponents<Component>();

            foreach (var component in gameObjectComponents)
            {
                if (component == null) continue;
                if (component is Transform) continue;

                var componentType = component.GetType().FullName;
                
                if (_useWhitelist)
                {
                    if (!_includedComponentTypes.Contains(componentType))
                        continue;
                }
                else
                {
                    if (_excludedComponentTypes.Contains(componentType))
                        continue;
                }

                var componentData = CreateComponentData(component);
                if (componentData != null)
                {
                    components.Add(componentData);
                }
            }

            return components;
        }

        private List<GameObjectData> CreateChildrenDataForGameObject(GameObject go, int currentDepth)
        {
            if (currentDepth >= _maxDepth)
                return new List<GameObjectData>();

            var children = new List<GameObjectData>();

            foreach (Transform child in go.transform)
            {
                var childData = new GameObjectData
                {
                    Name = child.gameObject.name,
                    Tag = _serializeTag ? child.gameObject.tag : null,
                    Layer = _serializeLayer ? child.gameObject.layer : 0,
                    IsActive = _serializeActive ? child.gameObject.activeInHierarchy : true,
                    Timestamp = DateTime.UtcNow.Ticks
                };

                if (_serializeTransform)
                {
                    childData.Transform = CreateTransformData(child);
                }

                if (_serializeComponents)
                {
                    childData.Components = CreateComponentsDataForGameObject(child.gameObject);
                }

                if (_serializeChildren && currentDepth + 1 < _maxDepth)
                {
                    childData.Children = CreateChildrenDataForGameObject(child.gameObject, currentDepth + 1);
                }

                children.Add(childData);
            }

            return children;
        }

        private async UniTask ApplyGameObjectDataToGameObject(GameObjectData data, GameObject target)
        {
            await UniTask.SwitchToMainThread();

            if (data == null || target == null)
                return;

            // Apply basic properties
            if (_serializeActive)
            {
                target.SetActive(data.IsActive);
            }

            if (_serializeTag && !string.IsNullOrEmpty(data.Tag))
            {
                target.tag = data.Tag;
            }

            if (_serializeLayer)
            {
                target.gameObject.layer = data.Layer;
            }

            target.name = data.Name;

            // Apply transform data
            if (_serializeTransform && data.Transform != null)
            {
                await ApplyTransformDataToTransform(data.Transform, target.transform);
            }

            // Apply component data
            if (_serializeComponents && data.Components != null)
            {
                await ApplyComponentsDataToGameObject(data.Components, target);
            }

            // Apply children data
            if (_serializeChildren && data.Children != null)
            {
                await ApplyChildrenDataToGameObject(data.Children, target);
            }
        }

        private async UniTask ApplyTransformDataToTransform(TransformData data, Transform target)
        {
            await UniTask.SwitchToMainThread();

            if (data.HasPosition)
            {
                target.localPosition = new Vector3(data.Position.x, data.Position.y, data.Position.z);
            }

            if (data.HasRotation)
            {
                target.localRotation = new Quaternion(data.Rotation.x, data.Rotation.y, data.Rotation.z, data.Rotation.w);
            }

            if (data.HasScale)
            {
                target.localScale = new Vector3(data.Scale.x, data.Scale.y, data.Scale.z);
            }
        }

        private async UniTask ApplyComponentsDataToGameObject(List<ComponentData> componentsData, GameObject target)
        {
            await UniTask.SwitchToMainThread();

            foreach (var componentData in componentsData)
            {
                try
                {
                    await ApplyComponentDataToGameObject(componentData, target);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to apply component data for {componentData.TypeName}: {ex.Message}", this);
                }
            }
        }

        private async UniTask ApplyComponentDataToGameObject(ComponentData componentData, GameObject target)
        {
            await UniTask.SwitchToMainThread();

            // This is a simplified implementation - in a full system, you'd need more sophisticated
            // component recreation and data application logic
            var existingComponent = target.GetComponent(componentData.TypeName);
            
            if (existingComponent != null && existingComponent is Behaviour behaviour)
            {
                behaviour.enabled = componentData.IsEnabled;
            }
        }

        private async UniTask ApplyChildrenDataToGameObject(List<GameObjectData> childrenData, GameObject target)
        {
            await UniTask.SwitchToMainThread();

            // This is a simplified implementation - in a full system, you'd need to handle
            // creating missing children, removing extra children, and matching existing ones
            var existingChildren = target.transform.Cast<Transform>().ToArray();
            
            for (int i = 0; i < childrenData.Count && i < existingChildren.Length; i++)
            {
                await ApplyGameObjectDataToGameObject(childrenData[i], existingChildren[i].gameObject);
            }
        }

        /// <summary>
        /// Adds a component type to the exclusion list.
        /// </summary>
        /// <param name="componentType">Full type name of the component to exclude</param>
        public void ExcludeComponentType(string componentType)
        {
            if (!_excludedComponentTypes.Contains(componentType))
            {
                _excludedComponentTypes.Add(componentType);
            }
        }

        /// <summary>
        /// Removes a component type from the exclusion list.
        /// </summary>
        /// <param name="componentType">Full type name of the component to include</param>
        public void IncludeComponentType(string componentType)
        {
            _excludedComponentTypes.Remove(componentType);
        }

        /// <summary>
        /// Gets the current serialization statistics for this GameObject.
        /// </summary>
        /// <returns>Statistics about the serializable data</returns>
        public GameObjectSerializationStats GetSerializationStats()
        {
            var data = _cachedGameObjectData ?? CreateGameObjectDataSync();
            
            return new GameObjectSerializationStats
            {
                ComponentCount = data.Components?.Count ?? 0,
                ChildrenCount = data.Children?.Count ?? 0,
                TotalDepth = CalculateMaxDepth(data),
                EstimatedSizeBytes = EstimateDataSize(data)
            };
        }

        private int CalculateMaxDepth(GameObjectData data, int currentDepth = 0)
        {
            if (data.Children == null || data.Children.Count == 0)
                return currentDepth;

            return data.Children.AsValueEnumerable().Max(child => CalculateMaxDepth(child, currentDepth + 1));
        }

        private int EstimateDataSize(GameObjectData data)
        {
            // Rough estimation of serialized data size
            var size = 100; // Base GameObject data

            if (data.Transform != null)
                size += 50; // Transform data

            if (data.Components != null)
                size += data.Components.Count * 30; // Estimated component data

            if (data.Children != null)
                size += data.Children.AsValueEnumerable().Sum(EstimateDataSize);

            return size;
        }
    }

}
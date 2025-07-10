using System.Reflection;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;
using AhBearStudios.Core.DependencyInjection.Services;

namespace AhBearStudios.Core.Unity.DependencyInjection
{
    /// <summary>
    /// MonoBehaviour base class that provides framework-agnostic dependency injection.
    /// Use this instead of framework-specific MonoBehaviour injection solutions.
    /// </summary>
    public abstract class DIMonoBehaviour : UnityEngine.MonoBehaviour
    {
        private static AttributeAdapterService _attributeService;
        private static IDependencyProvider _globalProvider;

        /// <summary>
        /// Gets or sets the global dependency provider for MonoBehaviour injection.
        /// This should be set during application startup.
        /// </summary>
        public static IDependencyProvider GlobalProvider
        {
            get => _globalProvider;
            set => _globalProvider = value;
        }

        /// <summary>
        /// Gets the attribute adapter service instance.
        /// </summary>
        protected static AttributeAdapterService AttributeService => 
            _attributeService ??= new AttributeAdapterService();

        /// <summary>
        /// Performs dependency injection on this MonoBehaviour using our framework-agnostic attributes.
        /// Call this in Awake() or Start() to inject dependencies.
        /// </summary>
        protected virtual void InjectDependencies()
        {
            if (_globalProvider == null)
            {
                UnityEngine.Debug.LogWarning($"[DIMonoBehaviour] No global dependency provider set. " +
                                           $"Set DIMonoBehaviour.GlobalProvider during startup. " +
                                           $"GameObject: {gameObject.name}");
                return;
            }

            try
            {
                var injectableMembers = AttributeService.GetInjectableMembers(GetType());
                
                foreach (var member in injectableMembers)
                {
                    InjectMember(member);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DIMonoBehaviour] Failed to inject dependencies on {gameObject.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Injects a specific member with its dependency.
        /// </summary>
        /// <param name="member">The injectable member to inject.</param>
        private void InjectMember(InjectableMember member)
        {
            try
            {
                switch (member.MemberType)
                {
                    case InjectableMemberType.Property:
                        InjectProperty((PropertyInfo)member.Member, member.Attribute);
                        break;
                    case InjectableMemberType.Field:
                        InjectField((FieldInfo)member.Member, member.Attribute);
                        break;
                    case InjectableMemberType.Method:
                        InjectMethod((MethodInfo)member.Member, member.Attribute);
                        break;
                    // Constructor injection is not applicable to MonoBehaviour
                }
            }
            catch (Exception ex)
            {
                if (!member.Attribute.Optional)
                {
                    throw new InvalidOperationException(
                        $"Failed to inject required dependency '{member.Member.Name}' on {GetType().Name}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Injects a property dependency.
        /// </summary>
        private void InjectProperty(PropertyInfo property, DIInjectAttribute attribute)
        {
            if (!property.CanWrite) return;

            var dependency = ResolveDependency(property.PropertyType, attribute);
            if (dependency != null)
            {
                property.SetValue(this, dependency);
            }
        }

        /// <summary>
        /// Injects a field dependency.
        /// </summary>
        private void InjectField(FieldInfo field, DIInjectAttribute attribute)
        {
            var dependency = ResolveDependency(field.FieldType, attribute);
            if (dependency != null)
            {
                field.SetValue(this, dependency);
            }
        }

        /// <summary>
        /// Injects method parameters.
        /// </summary>
        private void InjectMethod(MethodInfo method, DIInjectAttribute attribute)
        {
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveDependency(parameters[i].ParameterType, attribute);
            }

            method.Invoke(this, args);
        }

        /// <summary>
        /// Resolves a dependency using the global provider.
        /// </summary>
        private object ResolveDependency(Type dependencyType, DIInjectAttribute attribute)
        {
            if (!string.IsNullOrEmpty(attribute.Name))
            {
                // Try named resolution if supported
                var resolveNamedMethod = _globalProvider.GetType().GetMethod("ResolveNamed");
                if (resolveNamedMethod != null)
                {
                    var genericMethod = resolveNamedMethod.MakeGenericMethod(dependencyType);
                    return genericMethod.Invoke(_globalProvider, new object[] { attribute.Name });
                }
            }

            // Standard resolution
            if (attribute.Optional)
            {
                var tryResolveMethod = _globalProvider.GetType().GetMethod("TryResolve");
                if (tryResolveMethod != null)
                {
                    var genericMethod = tryResolveMethod.MakeGenericMethod(dependencyType);
                    var parameters = new object[] { null };
                    var success = (bool)genericMethod.Invoke(_globalProvider, parameters);
                    return success ? parameters[0] : null;
                }
            }

            // Required resolution
            var resolveMethod = _globalProvider.GetType().GetMethod("Resolve");
            var genericResolveMethod = resolveMethod?.MakeGenericMethod(dependencyType);
            return genericResolveMethod?.Invoke(_globalProvider, null);
        }
    }
}
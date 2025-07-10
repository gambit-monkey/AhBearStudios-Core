using System.Runtime.Serialization;

namespace AhBearStudios.Core.DependencyInjection.Exceptions;

    /// <summary>
    /// Exception thrown when a circular dependency is detected.
    /// </summary>
    [Serializable]
    public sealed class CircularDependencyException : DependencyInjectionException
    {
        /// <summary>
        /// Gets the types involved in the circular dependency.
        /// </summary>
        public Type[] DependencyChain { get; }

        /// <summary>
        /// Initializes a new instance of the CircularDependencyException class.
        /// </summary>
        /// <param name="dependencyChain">The chain of types forming the circular dependency.</param>
        public CircularDependencyException(Type[] dependencyChain) 
            : base(CreateMessage(dependencyChain))
        {
            DependencyChain = dependencyChain ?? throw new ArgumentNullException(nameof(dependencyChain));
        }

        /// <summary>
        /// Initializes a new instance of the CircularDependencyException class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        private CircularDependencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            DependencyChain = (Type[])info.GetValue(nameof(DependencyChain), typeof(Type[]));
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(DependencyChain), DependencyChain);
        }

        /// <summary>
        /// Creates an error message from the dependency chain.
        /// </summary>
        /// <param name="dependencyChain">The dependency chain.</param>
        /// <returns>A formatted error message.</returns>
        private static string CreateMessage(Type[] dependencyChain)
        {
            if (dependencyChain == null || dependencyChain.Length == 0)
                return "Circular dependency detected";

            var typeNames = new string[dependencyChain.Length];
            for (int i = 0; i < dependencyChain.Length; i++)
            {
                typeNames[i] = dependencyChain[i]?.Name ?? "null";
            }

            return $"Circular dependency detected: {string.Join(" -> ", typeNames)}";
        }
    }
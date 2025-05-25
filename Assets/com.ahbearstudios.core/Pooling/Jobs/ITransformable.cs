namespace AhBearStudios.Core.Pooling.Jobs
{
    /// <summary>
    /// Interface for types that can be transformed
    /// </summary>
    /// <typeparam name="T">The type itself</typeparam>
    public interface ITransformable<T> where T : struct
    {
        /// <summary>
        /// Transforms the value by the specified factor
        /// </summary>
        /// <param name="factor">Transformation factor</param>
        /// <returns>Transformed value</returns>
        T Transform(float factor);
    }
}
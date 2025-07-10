namespace AhBearStudios.Core.MessageBus.Configuration
{
    /// <summary>
    /// Enumeration of delivery types for batched messages.
    /// </summary>
    public enum DeliveryType
    {
        /// <summary>
        /// Fire-and-forget delivery with no confirmation required.
        /// </summary>
        FireAndForget,

        /// <summary>
        /// Delivery with confirmation required.
        /// </summary>
        WithConfirmation,

        /// <summary>
        /// Reliable delivery with retry logic.
        /// </summary>
        Reliable
    }
}
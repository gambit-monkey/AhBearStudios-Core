namespace AhBearStudios.Core.Pooling.Factories
{
    /// <summary>
    /// Provides access to pool factories
    /// </summary>
    public static class PoolFactories
    {
        private static PoolFactory _mainFactory;
        
        /// <summary>
        /// Gets the main pool factory
        /// </summary>
        public static PoolFactory Main
        {
            get
            {
                if (_mainFactory == null)
                {
                    _mainFactory = new PoolFactory();
                }
                return _mainFactory;
            }
        }
        
        /// <summary>
        /// Initializes all factories
        /// </summary>
        public static void Initialize()
        {
            if (_mainFactory == null)
            {
                _mainFactory = new PoolFactory();
            }
        }
        
        /// <summary>
        /// Resets all factories
        /// </summary>
        public static void Reset()
        {
            _mainFactory = null;
        }
    }
}
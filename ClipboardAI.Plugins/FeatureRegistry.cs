using System;
using System.Collections.Generic;
using System.Linq;

namespace ClipboardAI.Plugins
{
    /// <summary>
    /// Registry for plugin features that allows plugins to register their capabilities
    /// without the core application having direct references to them
    /// </summary>
    public class FeatureRegistry
    {
        private static readonly Lazy<FeatureRegistry> _instance = new Lazy<FeatureRegistry>(() => new FeatureRegistry());
        
        public static FeatureRegistry Instance => _instance.Value;
        
        private readonly Dictionary<string, List<IPlugin>> _featureProviders = new Dictionary<string, List<IPlugin>>();
        
        private FeatureRegistry()
        {
            // Private constructor for singleton
        }
        
        /// <summary>
        /// Register a plugin as a provider for a specific feature
        /// </summary>
        /// <param name="featureId">Unique identifier for the feature</param>
        /// <param name="plugin">Plugin that provides the feature</param>
        public void RegisterFeatureProvider(string featureId, IPlugin plugin)
        {
            if (string.IsNullOrEmpty(featureId))
                throw new ArgumentNullException(nameof(featureId));
                
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
                
            if (!_featureProviders.ContainsKey(featureId))
            {
                _featureProviders[featureId] = new List<IPlugin>();
            }
            
            if (!_featureProviders[featureId].Contains(plugin))
            {
                _featureProviders[featureId].Add(plugin);
            }
        }
        
        /// <summary>
        /// Check if a feature is enabled based on its feature type
        /// </summary>
        /// <param name="featureType">The feature type to check</param>
        /// <returns>True if the feature is enabled, false otherwise</returns>
        public bool IsEnabled(AIFeatureType featureType)
        {
            // Convert AIFeatureType to feature ID string
            string featureId = featureType.ToString();
            
            // Check if the feature is available
            return IsFeatureAvailable(featureId);
        }
        
        /// <summary>
        /// Get all plugins that provide a specific feature
        /// </summary>
        /// <param name="featureId">Unique identifier for the feature</param>
        /// <returns>Collection of plugins that provide the feature</returns>
        public IEnumerable<IPlugin> GetFeatureProviders(string featureId)
        {
            if (string.IsNullOrEmpty(featureId))
                throw new ArgumentNullException(nameof(featureId));
                
            if (_featureProviders.ContainsKey(featureId))
            {
                return _featureProviders[featureId];
            }
            
            return Enumerable.Empty<IPlugin>();
        }
        
        /// <summary>
        /// Check if a feature is available (has at least one provider)
        /// </summary>
        /// <param name="featureId">Unique identifier for the feature</param>
        /// <returns>True if the feature is available, false otherwise</returns>
        public bool IsFeatureAvailable(string featureId)
        {
            if (string.IsNullOrEmpty(featureId))
                return false;
                
            return _featureProviders.ContainsKey(featureId) && _featureProviders[featureId].Count > 0;
        }
        
        /// <summary>
        /// Get all registered feature IDs
        /// </summary>
        /// <returns>Collection of feature IDs</returns>
        public IEnumerable<string> GetRegisteredFeatures()
        {
            return _featureProviders.Keys;
        }
    }
}

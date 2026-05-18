using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace robot_controller_api.Persistence.Helpers
{
    // Simple reflection cache for public instance properties.
    public static class ReflectionCache
    {
        // Cache mapping Type -> array of PropertyInfo
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propsCache
            = new ConcurrentDictionary<Type, PropertyInfo[]>();

        // Returns public instance properties for the given type (cached).
        public static PropertyInfo[] GetPublicInstanceProperties(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            // Retrieve from cache or populate by reflecting the type's public instance properties
            return _propsCache.GetOrAdd(type, t =>
            {
                return t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            });
        }

        // Generic convenience wrapper.
        public static PropertyInfo[] GetPublicInstanceProperties<T>() => GetPublicInstanceProperties(typeof(T));
    }
}
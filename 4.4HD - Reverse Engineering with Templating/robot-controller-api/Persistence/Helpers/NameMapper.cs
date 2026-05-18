using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace robot_controller_api.Persistence.Helpers
{
    // Maps DB column names (snake_case) to CLR property names and caches lookups.
    public static class NameMapper
    {
        // Cache: Type -> (columnName -> PropertyInfo)
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> _columnToPropertyCache
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();

        // Convert snake_case to PascalCase 
        public static string ToPascal(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var parts = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1).ToLowerInvariant());
            }
            return sb.ToString();
        }

        // Convert PascalCase to snake_case 
        public static string ToSnake(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        // Build or return cached map: snake_case column -> PropertyInfo
        public static IReadOnlyDictionary<string, PropertyInfo> GetColumnToPropertyMap(Type type)
        {
            return _columnToPropertyCache.GetOrAdd(type, t =>
            {
                // Get public instance properties
                var props = ReflectionCache.GetPublicInstanceProperties(t);

                // Build mapping, prefer ColumnAttribute if present
                var dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in props)
                {
                    var colAttr = p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
                    string columnName = colAttr?.Name ?? ToSnake(p.Name);
                    if (!dict.ContainsKey(columnName))
                        dict[columnName] = p;
                }
                return dict;
            });
        }

        // Generic wrapper
        public static IReadOnlyDictionary<string, PropertyInfo> GetColumnToPropertyMap<T>() => GetColumnToPropertyMap(typeof(T));

        // Try to get PropertyInfo for a given column name
        public static bool TryGetPropertyForColumn(Type type, string columnName, out PropertyInfo? property)
        {
            property = null;
            if (string.IsNullOrEmpty(columnName)) return false;
            var map = GetColumnToPropertyMap(type);
            return map.TryGetValue(columnName, out property);
        }

        // Generic version
        public static bool TryGetPropertyForColumn<T>(string columnName, out PropertyInfo? property)
        {
            return TryGetPropertyForColumn(typeof(T), columnName, out property);
        }

        // Get property value by column name (throws if not found)
        public static object? GetValueByColumn(object target, string columnName)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
            var type = target.GetType();
            if (!TryGetPropertyForColumn(type, columnName, out var prop) || prop == null)
                throw new KeyNotFoundException($"No property mapped for column '{columnName}' on type {type.FullName}.");
            return prop.GetValue(target);
        }

        // Set property value by column name (converts types when needed)
        public static void SetValueByColumn(object target, string columnName, object? value)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException(nameof(columnName));
            var type = target.GetType();
            if (!TryGetPropertyForColumn(type, columnName, out var prop) || prop == null)
                throw new KeyNotFoundException($"No property mapped for column '{columnName}' on type {type.FullName}.");
            if (!prop.CanWrite) throw new InvalidOperationException($"Property '{prop.Name}' is not writable.");
            // Convert value to property type if necessary
            if (value != null && prop.PropertyType.IsAssignableFrom(value.GetType()) == false)
            {
                value = Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            prop.SetValue(target, value);
        }
    }
}
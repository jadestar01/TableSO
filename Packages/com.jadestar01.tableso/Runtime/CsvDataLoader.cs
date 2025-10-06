using System;
using System.Collections.Generic;
using System.Reflection;
using TableSO.FileUtility;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TableSO.Scripts.Generator
{
    public static class CsvDataLoader
    {
        // Type cache to avoid repeated reflection calls
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, ConstructorInfo> _constructorCache = new Dictionary<Type, ConstructorInfo>();
        private static readonly Dictionary<Type, Dictionary<string, Type[]>> _constructorSignatureCache = new Dictionary<Type, Dictionary<string, Type[]>>();
        private static Type[] _cachedAssemblyTypes = null;
        private static readonly object _cacheLock = new object();

        public static async Task<List<T>> LoadCsvDataAsync<T>(string csvPath) where T : class
        {
            string addressKey = $"{FilePath.CSV_PATH}{csvPath}.csv";

            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(addressKey);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[TableSO] Fail to Load Addressable : {addressKey}");
                return null;
            }

            string csvText = handle.Result.text;
            string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 3) 
            {
                Debug.LogWarning($"[TableSO] {csvPath}.csv: No data found");
                return new List<T>();
            }

            string[] fieldNames = ParseCsvLine(lines[0]);
            string[] fieldTypes = ParseCsvLine(lines[1]);
            
            int dataRowCount = lines.Length - 2;
            List<T> dataList = new List<T>(dataRowCount);

            string constructorKey = string.Join("|", fieldTypes);
            ConstructorInfo constructor = GetCachedConstructor<T>(constructorKey, fieldTypes, fieldNames);
            
            if (constructor == null)
            {
                return null; 
            }

            Type[] parameterTypes = new Type[fieldTypes.Length];
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                parameterTypes[i] = GetTypeFromString(fieldTypes[i]);
            }

            object[] constructorArgs = new object[fieldNames.Length];

            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = ParseCsvLine(lines[i]);

                if (values.Length != fieldNames.Length)
                {
                    Debug.LogWarning($"[TableSO] {csvPath}.csv row {i + 1}: Field count mismatch. Expected: {fieldNames.Length}, Got: {values.Length}");
                    continue;
                }

                for (int j = 0; j < values.Length; j++)
                {
                    constructorArgs[j] = ConvertValue(values[j], fieldTypes[j], parameterTypes[j]);
                }

                try
                {
                    T dataInstance = (T)constructor.Invoke(constructorArgs);
                    dataList.Add(dataInstance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] Constructor invocation failed at row {i + 1}: {e.Message}");
                }
            }

            return dataList;
        }

        private static ConstructorInfo GetCachedConstructor<T>(string cacheKey, string[] fieldTypes, string[] fieldNames)
        {
            Type dataType = typeof(T);
            
            if (!_constructorSignatureCache.TryGetValue(dataType, out var signatureCache))
            {
                signatureCache = new Dictionary<string, Type[]>();
                _constructorSignatureCache[dataType] = signatureCache;
            }

            if (signatureCache.TryGetValue(cacheKey, out Type[] cachedTypes))
            {
                if (_constructorCache.TryGetValue(dataType, out ConstructorInfo cachedConstructor))
                {
                    return cachedConstructor;
                }
            }

            ConstructorInfo constructor = FindMatchingConstructor(dataType, fieldTypes, fieldNames);
            if (constructor != null)
            {
                _constructorCache[dataType] = constructor;
                signatureCache[cacheKey] = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            }

            return constructor;
        }

        private static ConstructorInfo FindMatchingConstructor(Type dataType, string[] fieldTypes, string[] fieldNames)
        {
            ConstructorInfo[] constructors = dataType.GetConstructors();

            if (constructors.Length == 0)
            {
                Debug.LogError($"[TableSO] No public constructors found");
                return null;
            }

            int targetParamCount = fieldTypes.Length;
            ConstructorInfo[] matchingCountConstructors = new ConstructorInfo[constructors.Length];
            int matchCount = 0;

            for (int i = 0; i < constructors.Length; i++)
            {
                if (constructors[i].GetParameters().Length == targetParamCount)
                {
                    matchingCountConstructors[matchCount++] = constructors[i];
                }
            }

            if (matchCount == 0)
            {
                Debug.LogError($"[TableSO] No constructor with {targetParamCount} parameters found");
                return null;
            }

            for (int i = 0; i < matchCount; i++)
            {
                var parameters = matchingCountConstructors[i].GetParameters();
                bool typeMatch = true;

                for (int j = 0; j < parameters.Length; j++)
                {
                    Type expectedType = GetTypeFromString(fieldTypes[j]);
                    Type paramType = parameters[j].ParameterType;

                    if (!IsTypeCompatible(expectedType, paramType))
                    {
                        typeMatch = false;
                        break;
                    }
                }

                if (typeMatch)
                {
                    return matchingCountConstructors[i];
                }
            }

            var fallbackConstructor = matchingCountConstructors[0];
            Debug.LogWarning($"[TableSO] No exact type match found. Using fallback constructor");
            return fallbackConstructor;
        }

        private static bool IsTypeCompatible(Type expectedType, Type actualType)
        {
            if (expectedType == actualType) return true;
            if (expectedType == null || actualType == null) return false;

            if (expectedType.IsArray && actualType.IsArray)
            {
                return IsTypeCompatible(expectedType.GetElementType(), actualType.GetElementType());
            }

            if (actualType.IsAssignableFrom(expectedType)) return true;
            if (expectedType.IsAssignableFrom(actualType)) return true;

            // Fast numeric type check
            if (IsNumericType(expectedType) && IsNumericType(actualType)) return true;

            return false;
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(float) || type == typeof(double) || 
                   type == typeof(decimal) || type == typeof(long) || type == typeof(short) || 
                   type == typeof(byte);
        }

        private static string[] ParseCsvLine(string line)
        {
            List<string> fields = new List<string>(16);
            bool inQuotes = false;
            int fieldStart = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(line.Substring(fieldStart, i - fieldStart).Trim());
                    fieldStart = i + 1;
                }
            }

            // Add last field
            if (fieldStart < line.Length)
            {
                fields.Add(line.Substring(fieldStart).Trim());
            }
            else
            {
                fields.Add(string.Empty);
            }

            return fields.ToArray();
        }

        private static Type GetTypeFromString(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return typeof(string);

            if (_typeCache.TryGetValue(typeString, out Type cachedType))
            {
                return cachedType;
            }

            Type resultType;

            if (typeString.EndsWith("[]"))
            {
                string elementTypeString = typeString.Substring(0, typeString.Length - 2).Trim();
                Type elementType = GetSingleTypeFromString(elementTypeString);
                resultType = elementType != null ? elementType.MakeArrayType() : typeof(string[]);
            }
            else
            {
                resultType = GetSingleTypeFromString(typeString);
            }

            _typeCache[typeString] = resultType;
            return resultType;
        }

        private static Type GetSingleTypeFromString(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return typeof(string);

            string normalizedType = typeString.ToLower();

            switch (normalizedType)
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "string": return typeof(string);
                case "bool": return typeof(bool);
                case "double": return typeof(double);
                default:
                    Type foundType = FindUserDefinedType(typeString);
                    return foundType ?? typeof(string);
            }
        }

        private static Type FindUserDefinedType(string typeName)
        {
            if (_cachedAssemblyTypes == null)
            {
                lock (_cacheLock)
                {
                    if (_cachedAssemblyTypes == null)
                    {
                        InitializeAssemblyTypesCache();
                    }
                }
            }

            for (int i = 0; i < _cachedAssemblyTypes.Length; i++)
            {
                Type type = _cachedAssemblyTypes[i];
                if (type.Name == typeName && type.IsPublic && !type.IsNested)
                {
                    return type;
                }
            }

            return null;
        }

        private static void InitializeAssemblyTypesCache()
        {
            HashSet<string> excludedAssemblies = new HashSet<string>
            {
                "UnityEngine", "UnityEditor", "Unity.Collections", "Unity.Mathematics",
                "Unity.Burst", "Unity.Jobs", "UnityEngine.UI", "UnityEngine.CoreModule",
                "UnityEditor.CoreModule"
            };

            List<Type> allTypes = new List<Type>(128);

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    string assemblyName = assemblies[i].GetName().Name;
                    
                    bool shouldExclude = false;
                    foreach (string excluded in excludedAssemblies)
                    {
                        if (assemblyName.StartsWith(excluded))
                        {
                            shouldExclude = true;
                            break;
                        }
                    }

                    if (shouldExclude) continue;

                    Type[] types = assemblies[i].GetTypes();
                    allTypes.AddRange(types);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TableSO] Error loading types from assembly: {e.Message}");
                }
            }

            _cachedAssemblyTypes = allTypes.ToArray();
        }

        private static object ConvertValue(string value, string targetType, Type targetTypeObj)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType, targetTypeObj);

            if (targetType.EndsWith("[]"))
            {
                string elementTypeString = targetType.Substring(0, targetType.Length - 2).Trim();
                return ConvertToArray(value, elementTypeString, targetTypeObj.GetElementType());
            }

            return ConvertSingleValue(value, targetType, targetTypeObj);
        }

        private static object ConvertToArray(string value, string elementType, Type elementTypeObj)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.CreateInstance(elementTypeObj, 0);
            }

            string[] elements = value.Split('|');
            Array resultArray = Array.CreateInstance(elementTypeObj, elements.Length);

            for (int i = 0; i < elements.Length; i++)
            {
                string trimmed = elements[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    object convertedElement = ConvertSingleValue(trimmed, elementType, elementTypeObj);
                    resultArray.SetValue(convertedElement, i);
                }
                else
                {
                    resultArray.SetValue(GetSingleDefaultValue(elementType, elementTypeObj), i);
                }
            }

            return resultArray;
        }

        private static object ConvertSingleValue(string value, string targetType, Type targetTypeObj)
        {
            if (string.IsNullOrEmpty(value)) return GetSingleDefaultValue(targetType, targetTypeObj);

            string normalizedType = targetType.ToLower();

            try
            {
                switch (normalizedType)
                {
                    case "int": return int.Parse(value);
                    case "float": return float.Parse(value);
                    case "string": return value;
                    case "bool": return bool.Parse(value);
                    case "double": return double.Parse(value);
                    default:
                        if (targetTypeObj != null && targetTypeObj.IsEnum)
                        {
                            try
                            {
                                return Enum.Parse(targetTypeObj, value, true);
                            }
                            catch (ArgumentException)
                            {
                                Array enumValues = Enum.GetValues(targetTypeObj);
                                return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                            }
                        }
                        return value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TableSO] Cannot convert value '{value}' to {targetType}: {e.Message}");
                return GetSingleDefaultValue(targetType, targetTypeObj);
            }
        }

        private static object GetDefaultValue(string typeString, Type typeObj)
        {
            if (typeString.EndsWith("[]"))
            {
                Type elementType = typeObj?.GetElementType() ?? typeof(string);
                return Array.CreateInstance(elementType, 0);
            }

            return GetSingleDefaultValue(typeString, typeObj);
        }

        private static object GetSingleDefaultValue(string typeString, Type typeObj)
        {
            string normalizedType = typeString.ToLower();

            switch (normalizedType)
            {
                case "int": return 0;
                case "float": return 0f;
                case "string": return string.Empty;
                case "bool": return false;
                case "double": return 0.0;
                default:
                    if (typeObj != null && typeObj.IsEnum)
                    {
                        Array enumValues = Enum.GetValues(typeObj);
                        return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                    }
                    return null;
            }
        }

        public static void ClearCaches()
        {
            _typeCache.Clear();
            _constructorCache.Clear();
            _constructorSignatureCache.Clear();
            _cachedAssemblyTypes = null;
        }
    }
}
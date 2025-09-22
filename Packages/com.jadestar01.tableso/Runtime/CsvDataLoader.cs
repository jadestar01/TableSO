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
        public static async Task<List<T>> LoadCsvDataAsync<T>(string csvPath) where T : class
        {
            List<T> dataList = new List<T>();

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
                return dataList;
            }

            string[] fieldNames = ParseCsvLine(lines[0]);
            string[] fieldTypes = ParseCsvLine(lines[1]);

            ConstructorInfo constructor = FindMatchingConstructor(typeof(T), fieldTypes, fieldNames);
            if (constructor == null)
            {
                return null; 
            }

            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                string[] values = ParseCsvLine(lines[i]);

                if (values.Length != fieldNames.Length)
                {
                    Debug.LogWarning($"[TableSO] {csvPath}.csv row {i + 1}: Field count mismatch. Expected: {fieldNames.Length}, Got: {values.Length}");
                    continue;
                }

                object[] constructorArgs = new object[values.Length];

                for (int j = 0; j < values.Length; j++)
                {
                    constructorArgs[j] = ConvertValue(values[j], fieldTypes[j]);
                }

                try
                {
                    T dataInstance = (T)constructor.Invoke(constructorArgs);
                    dataList.Add(dataInstance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] Constructor args: [{string.Join(", ", constructorArgs.Select(arg => arg?.ToString() ?? "null"))}]");
                }
            }

            return dataList;
        }
        
        private static ConstructorInfo FindMatchingConstructor(Type dataType, string[] fieldTypes, string[] fieldNames)
        {
            ConstructorInfo[] constructors = dataType.GetConstructors();
            
            if (constructors.Length == 0)
            {
                Debug.LogError($"[TableSO] No public constructors found");
                return null;
            }

            for (int i = 0; i < constructors.Length; i++)
            {
                var parameters = constructors[i].GetParameters();
                string paramInfo = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            }

            var matchingCountConstructors = constructors.Where(c => c.GetParameters().Length == fieldTypes.Length).ToArray();
            
            if (matchingCountConstructors.Length == 0)
            {
                Debug.LogError($"[TableSO] No constructor with {fieldTypes.Length} parameters found " +
                              $"CSV has {fieldTypes.Length} fields but available constructors have: {string.Join(", ", constructors.Select(c => $"{c.GetParameters().Length}"))} parameters");
                return null;
            }

            foreach (var constructor in matchingCountConstructors)
            {
                var parameters = constructor.GetParameters();
                bool typeMatch = true;
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type expectedType = GetTypeFromString(fieldTypes[i]);
                    Type paramType = parameters[i].ParameterType;
                    
                    if (!IsTypeCompatible(expectedType, paramType))
                    {
                        Debug.LogWarning($"[TableSO] Type mismatch at parameter {i}: expected {expectedType?.Name ?? "null"}, got {paramType.Name}");
                        typeMatch = false;
                        break;
                    }
                }
                
                if (typeMatch)
                {
                    return constructor;
                }
            }

            // 3. 타입이 완전히 일치하지 않는 경우, 첫 번째 생성자 사용하고 경고 출력
            var fallbackConstructor = matchingCountConstructors[0];
            var fallbackParams = fallbackConstructor.GetParameters();
            
            Debug.LogWarning($"[TableSO] No exact type match found for constructor. Using fallback constructor:");
            Debug.LogWarning($"Expected types: [{string.Join(", ", fieldTypes)}]");
            Debug.LogWarning($"Constructor types: [{string.Join(", ", fallbackParams.Select(p => p.ParameterType.Name))}]");
            
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
            
            try
            {
                if (actualType.IsAssignableFrom(expectedType)) return true;
                if (expectedType.IsAssignableFrom(actualType)) return true;
                
                var numericTypes = new[] { typeof(int), typeof(float), typeof(double), typeof(decimal), typeof(long), typeof(short), typeof(byte) };
                if (numericTypes.Contains(expectedType) && numericTypes.Contains(actualType)) return true;
            }
            catch
            {
            }
            
            return false;
        }

        private static string[] ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            System.Text.StringBuilder currentField = new System.Text.StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            
            fields.Add(currentField.ToString().Trim());
            return fields.ToArray();
        }
        
        private static Type GetTypeFromString(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return typeof(string);
            
            // Handle array types
            if (typeString.EndsWith("[]"))
            {
                string elementTypeString = typeString.Substring(0, typeString.Length - 2).Trim();
                Type elementType = GetSingleTypeFromString(elementTypeString);
                if (elementType != null)
                {
                    return elementType.MakeArrayType();
                }
                return typeof(string[]); // Default value
            }

            return GetSingleTypeFromString(typeString);
        }

        private static Type GetSingleTypeFromString(string typeString)
        {
            if (string.IsNullOrEmpty(typeString)) return typeof(string);
            
            string normalizedType = typeString.ToLower().Trim();
            
            switch (normalizedType)
            {
                case "int":
                    return typeof(int);
                case "float":
                    return typeof(float);
                case "string":
                    return typeof(string);
                case "bool":
                    return typeof(bool);
                case "double":
                    return typeof(double);
                default:
                    Type foundType = FindUserDefinedType(typeString);
                    return foundType ?? typeof(string); // Default value
            }
        }

        private static Type FindUserDefinedType(string typeName)
        {
            HashSet<string> excludedAssemblies = new HashSet<string>
            {
                "UnityEngine",
                "UnityEditor", 
                "Unity.Collections",
                "Unity.Mathematics",
                "Unity.Burst",
                "Unity.Jobs",
                "UnityEngine.UI",
                "UnityEngine.CoreModule",
                "UnityEditor.CoreModule"
            };

            List<Type> candidateTypes = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string assemblyName = assembly.GetName().Name;
                    if (excludedAssemblies.Any(excluded => assemblyName.StartsWith(excluded)))
                    {
                        continue;
                    }

                    Type type = assembly.GetType(typeName);
                    if (type != null && type.IsPublic && !type.IsNested)
                    {
                        candidateTypes.Add(type);
                    }
                    
                    foreach (Type assemblyType in assembly.GetTypes())
                    {
                        if (assemblyType.Name == typeName && assemblyType.IsPublic && !assemblyType.IsNested)
                        {
                            if (!candidateTypes.Contains(assemblyType))
                            {
                                candidateTypes.Add(assemblyType);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TableSO] Error loading types from assembly {assembly.FullName}: {e.Message}");
                }
            }

            if (candidateTypes.Count > 0)
            {
                Type selectedType = candidateTypes[0];
                
                return selectedType;
            }

            Debug.LogWarning($"[TableSO] Cannot find user-defined type '{typeName}'");
            return null;
        }

        private static object ConvertValue(string value, string targetType)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType);
            
            // Handle array types
            if (targetType.EndsWith("[]"))
            {
                string elementTypeString = targetType.Substring(0, targetType.Length - 2).Trim();
                return ConvertToArray(value, elementTypeString);
            }

            return ConvertSingleValue(value, targetType);
        }

        private static object ConvertToArray(string value, string elementType)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Return empty array
                Type _elementTypeObj = GetSingleTypeFromString(elementType);
                return Array.CreateInstance(_elementTypeObj, 0);
            }

            // Parse values separated by pipe (|)
            string[] elements = value.Split('|').Select(s => s.Trim()).ToArray();
            Type elementTypeObj = GetSingleTypeFromString(elementType);
            Array resultArray = Array.CreateInstance(elementTypeObj, elements.Length);

            for (int i = 0; i < elements.Length; i++)
            {
                if (!string.IsNullOrEmpty(elements[i]))
                {
                    object convertedElement = ConvertSingleValue(elements[i], elementType);
                    resultArray.SetValue(convertedElement, i);
                }
                else
                {
                    // Set empty elements to default value
                    object defaultValue = GetSingleDefaultValue(elementType);
                    resultArray.SetValue(defaultValue, i);
                }
            }

            return resultArray;
        }

        private static object ConvertSingleValue(string value, string targetType)
        {
            if (string.IsNullOrEmpty(value)) return GetSingleDefaultValue(targetType);
            
            string normalizedType = targetType.ToLower().Trim();
            
            try
            {
                switch (normalizedType)
                {
                    case "int":
                        return int.Parse(value);
                    case "float":
                        return float.Parse(value);
                    case "string":
                        return value;
                    case "bool":
                        return bool.Parse(value);
                    case "double":
                        return double.Parse(value);
                    default:
                        // Handle enum
                        Type enumType = GetSingleTypeFromString(targetType);
                        if (enumType != null && enumType.IsEnum)
                        {
                            try
                            {
                                return Enum.Parse(enumType, value, true); // ignoreCase = true
                            }
                            catch (ArgumentException)
                            {
                                Debug.LogWarning($"[TableSO] Cannot find enum value '{value}' in {targetType}. Using first value");
                                Array enumValues = Enum.GetValues(enumType);
                                return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                            }
                        }
                        return value; // Return as string
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TableSO] Cannot convert value '{value}' to {targetType}: {e.Message}");
                return GetSingleDefaultValue(targetType);
            }
        }

        private static object GetDefaultValue(string typeString)
        {
            // For array types
            if (typeString.EndsWith("[]"))
            {
                string elementTypeString = typeString.Substring(0, typeString.Length - 2).Trim();
                Type elementType = GetSingleTypeFromString(elementTypeString);
                return Array.CreateInstance(elementType, 0); // Empty array
            }

            return GetSingleDefaultValue(typeString);
        }

        private static object GetSingleDefaultValue(string typeString)
        {
            string normalizedType = typeString.ToLower().Trim();
            
            switch (normalizedType)
            {
                case "int":
                    return 0;
                case "float":
                    return 0f;
                case "string":
                    return string.Empty;
                case "bool":
                    return false;
                case "double":
                    return 0.0;
                default:
                    Type enumType = GetSingleTypeFromString(typeString);
                    if (enumType != null && enumType.IsEnum)
                    {
                        Array enumValues = Enum.GetValues(enumType);
                        return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                    }
                    return null;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TableSO.FileUtility;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace TableSO.Scripts.Generator
{
    public static class CSVDataLoader
    {
        public static void LoadAllCSVData()
        {
            string csvDataPath = FilePath.CSV_PATH;
            string tableOutputPath = FilePath.TABLE_OUTPUT_PATH;
            
            if (!Directory.Exists(csvDataPath))
            {
                Debug.LogError($"[TableSO] CSV Data folder does not exist: {csvDataPath}");
                return;
            }

            string[] csvFiles = Directory.GetFiles(csvDataPath, "*.csv");

            List<ScriptableObject> sos = new();
            
            foreach (string csvPath in csvFiles)
            {
                var tableSO = LoadCSVDataToTableSO(csvPath, tableOutputPath);
                if (tableSO != null)
                {
                    sos.Add(tableSO);
                }
            }
            
            RegisterTablesToCenter(sos);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static void RegisterTablesToCenter(List<ScriptableObject> tables)
        {
            if (tables.Count == 0) return;
            
            // Find TableCenter
            string[] guids = AssetDatabase.FindAssets("t:TableCenter");
            
            TableCenter tableCenter = null;
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                tableCenter = AssetDatabase.LoadAssetAtPath<TableCenter>(path);
            }
            
            if (tableCenter == null)
            {
                Debug.LogWarning("[TableSO] Cannot find TableCenter. Please register manually");
                return;
            }
            
            // Register tables
            foreach (var table in tables)
            {
                if (table != null)
                {
                    tableCenter.RegisterTable(table);
                }
            }
            
            EditorUtility.SetDirty(tableCenter);
        }

        private static ScriptableObject LoadCSVDataToTableSO(string csvPath, string tableOutputPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            string tableSOPath = Path.Combine(tableOutputPath, $"{fileName}TableSO.asset");
            ScriptableObject tableSO = AssetDatabase.LoadAssetAtPath<ScriptableObject>(tableSOPath);

            try
            {
                if (tableSO == null)
                {
                    // Find TableSO type
                    Type tableSOType = FindTableSOType($"{fileName}TableSO");
                    if (tableSOType == null)
                    {
                        Debug.LogError($"[TableSO] Cannot find {fileName}TableSO type. Please generate code first");
                        return null;
                    }
                    
                    tableSO = ScriptableObject.CreateInstance(tableSOType);
                    AssetDatabase.CreateAsset(tableSO, tableSOPath);
                }

                // Load CSV data
                List<object> dataList = LoadCSVData(csvPath, fileName);
                
                if (dataList == null)
                {
                    Debug.LogError($"[TableSO] Failed to load CSV data for {fileName}");
                    return null;
                }
                
                // Set data to TableSO's dataList field
                SetTableSOData(tableSO, dataList);
                
                EditorUtility.SetDirty(tableSO);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error loading {fileName} CSV: {e.Message}\nStack trace: {e.StackTrace}");
                return null;
            }
            
            return tableSO;
        }

        private static List<object> LoadCSVData(string csvPath, string className)
        {
            List<object> dataList = new List<object>();
            string[] lines = File.ReadAllLines(csvPath);
            
            if (lines.Length < 3) // Header, type, minimum 1 data
            {
                Debug.LogWarning($"[TableSO] {className}.csv: No data found");
                return dataList;
            }

            string[] fieldNames = ParseCSVLine(lines[0]);
            string[] fieldTypes = ParseCSVLine(lines[1]);
            
            // Find data class type
            Type dataType = FindDataClassType(className);
            if (dataType == null)
            {
                Debug.LogError($"[TableSO] Cannot find {className} data class");
                return null;
            }
            
            // Find matching constructor
            ConstructorInfo constructor = FindMatchingConstructor(dataType, fieldTypes, fieldNames, className);
            if (constructor == null)
            {
                return null; // 에러 메시지는 FindMatchingConstructor 내에서 출력
            }

            // Process data rows (starting from row 3)
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                
                if (values.Length != fieldNames.Length)
                {
                    Debug.LogWarning($"[TableSO] {className}.csv row {i+1}: Field count mismatch. Expected: {fieldNames.Length}, Got: {values.Length}");
                    continue;
                }

                object[] constructorArgs = new object[values.Length];
                
                for (int j = 0; j < values.Length; j++)
                {
                    constructorArgs[j] = ConvertValue(values[j], fieldTypes[j]);
                }
                
                try
                {
                    object dataInstance = constructor.Invoke(constructorArgs);
                    dataList.Add(dataInstance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] Error creating instance for {className} row {i+1}: {e.Message}");
                    Debug.LogError($"[TableSO] Constructor args: [{string.Join(", ", constructorArgs.Select(arg => arg?.ToString() ?? "null"))}]");
                }
            }
            
            return dataList;
        }

        private static ConstructorInfo FindMatchingConstructor(Type dataType, string[] fieldTypes, string[] fieldNames, string className)
        {
            ConstructorInfo[] constructors = dataType.GetConstructors();
            
            if (constructors.Length == 0)
            {
                Debug.LogError($"[TableSO] No public constructors found for {className}");
                return null;
            }

            for (int i = 0; i < constructors.Length; i++)
            {
                var parameters = constructors[i].GetParameters();
                string paramInfo = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            }

            // 1. 매개변수 개수가 일치하는 생성자부터 찾기
            var matchingCountConstructors = constructors.Where(c => c.GetParameters().Length == fieldTypes.Length).ToArray();
            
            if (matchingCountConstructors.Length == 0)
            {
                Debug.LogError($"[TableSO] No constructor with {fieldTypes.Length} parameters found for {className}. " +
                              $"CSV has {fieldTypes.Length} fields but available constructors have: {string.Join(", ", constructors.Select(c => $"{c.GetParameters().Length}"))} parameters");
                return null;
            }

            // 2. 타입이 정확히 일치하는 생성자 찾기
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
            
            Debug.LogWarning($"[TableSO] No exact type match found for {className} constructor. Using fallback constructor:");
            Debug.LogWarning($"Expected types: [{string.Join(", ", fieldTypes)}]");
            Debug.LogWarning($"Constructor types: [{string.Join(", ", fallbackParams.Select(p => p.ParameterType.Name))}]");
            
            return fallbackConstructor;
        }

        private static bool IsTypeCompatible(Type expectedType, Type actualType)
        {
            if (expectedType == actualType) return true;
            
            // null 허용
            if (expectedType == null || actualType == null) return false;
            
            // 배열 타입 검사
            if (expectedType.IsArray && actualType.IsArray)
            {
                return IsTypeCompatible(expectedType.GetElementType(), actualType.GetElementType());
            }
            
            // 기본 타입 변환 가능한지 검사
            try
            {
                if (actualType.IsAssignableFrom(expectedType)) return true;
                if (expectedType.IsAssignableFrom(actualType)) return true;
                
                // 숫자 타입 간 호환성 검사
                var numericTypes = new[] { typeof(int), typeof(float), typeof(double), typeof(decimal), typeof(long), typeof(short), typeof(byte) };
                if (numericTypes.Contains(expectedType) && numericTypes.Contains(actualType)) return true;
            }
            catch
            {
                // 예외 발생 시 false 반환
            }
            
            return false;
        }

        private static string[] ParseCSVLine(string line)
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

        private static Type FindTableSOType(string typeName)
        {
            Type type = null;
            
            // 첫 번째 시도
            type = FindTypeInAssemblies($"Table.{typeName}");
            
            if (type == null)
            {
                // 어셈블리 새로고침 후 재시도
                Debug.LogWarning($"[TableSO] Type {typeName} not found, refreshing assemblies...");
                AssetDatabase.Refresh();
                System.Threading.Thread.Sleep(1000); // 잠시 대기
                
                type = FindTypeInAssemblies($"Table.{typeName}");
            }
            
            return type;
        }

        private static Type FindDataClassType(string className)
        {
            Type type = null;
            
            // 1. 기본 네임스페이스로 시도
            type = FindTypeInAssemblies($"TableData.{className}");
            
            if (type == null)
            {
                // 2. 네임스페이스 없이 시도
                type = FindTypeInAssemblies(className);
            }
            
            if (type == null)
            {
                // 3. 어셈블리 새로고침 후 재시도
                Debug.LogWarning($"[TableSO] Type {className} not found, refreshing assemblies...");
                AssetDatabase.Refresh();
                System.Threading.Thread.Sleep(1000);
                
                type = FindTypeInAssemblies($"TableData.{className}");
                if (type == null)
                {
                    type = FindTypeInAssemblies(className);
                }
            }
            
            return type;
        }

        private static Type FindTypeInAssemblies(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(typeName);
                    if (type != null) return type;
                }
                catch (Exception e)
                {
                    // 어셈블리 로딩 오류는 무시하고 계속 진행
                    Debug.LogWarning($"[TableSO] Error loading type from assembly {assembly.FullName}: {e.Message}");
                }
            }
            return null;
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
                    // Handle enum or custom types - 사용자 정의 타입만 찾기
                    Type foundType = FindUserDefinedType(typeString);
                    return foundType ?? typeof(string); // Default value
            }
        }

        private static Type FindUserDefinedType(string typeName)
        {
            // Unity 내부 어셈블리 제외 목록
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
                    // Unity 내부 어셈블리 건너뛰기
                    string assemblyName = assembly.GetName().Name;
                    if (excludedAssemblies.Any(excluded => assemblyName.StartsWith(excluded)))
                    {
                        continue;
                    }

                    // 정확한 타입 이름으로 찾기
                    Type type = assembly.GetType(typeName);
                    if (type != null && type.IsPublic && !type.IsNested)
                    {
                        candidateTypes.Add(type);
                    }
                    
                    // 네임스페이스 없이 타입 이름으로 찾기
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
                    // 어셈블리 로딩 오류는 무시하고 계속 진행
                    Debug.LogWarning($"[TableSO] Error loading types from assembly {assembly.FullName}: {e.Message}");
                }
            }

            if (candidateTypes.Count > 0)
            {
                // 첫 번째 후보 사용 (가장 적합한 것으로 추정)
                Type selectedType = candidateTypes[0];
                
                if (candidateTypes.Count > 1)
                {
                    Debug.LogWarning($"[TableSO] Multiple types found for '{typeName}', using: {selectedType.FullName}");
                }
                
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
                    // Handle enum default values
                    Type enumType = GetSingleTypeFromString(typeString);
                    if (enumType != null && enumType.IsEnum)
                    {
                        Array enumValues = Enum.GetValues(enumType);
                        return enumValues.Length > 0 ? enumValues.GetValue(0) : null;
                    }
                    return null;
            }
        }

        private static void SetTableSOData(ScriptableObject tableSO, List<object> dataList)
        {
            Type tableSOType = tableSO.GetType();
            FieldInfo dataListField = null;
            
            // Find dataList field in parent class
            Type currentType = tableSOType;
            while (currentType != null && dataListField == null)
            {
                dataListField = currentType.GetField("dataList", BindingFlags.NonPublic | BindingFlags.Instance);
                currentType = currentType.BaseType;
            }
            
            if (dataListField != null)
            {
                // Create new list of type List<TData>
                Type dataType = dataListField.FieldType.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(dataType);
                object newList = Activator.CreateInstance(listType);
                
                // Add data
                MethodInfo addMethod = listType.GetMethod("Add");
                foreach (object data in dataList)
                {
                    addMethod.Invoke(newList, new[] { data });
                }
                
                dataListField.SetValue(tableSO, newList);
                
                // Set isUpdated flag - 현재 타입에서 찾기
                Type searchType = tableSOType;
                while (searchType != null)
                {
                    FieldInfo isUpdatedField = searchType.GetField("isUpdated", BindingFlags.Public | BindingFlags.Instance);
                    if (isUpdatedField != null)
                    {
                        isUpdatedField.SetValue(tableSO, true);
                        break;
                    }
                    searchType = searchType.BaseType;
                }

                MethodInfo onDataUpdatedMethod = tableSOType.GetMethod("OnDataUpdated",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (onDataUpdatedMethod != null)
                {
                    onDataUpdatedMethod.Invoke(tableSO, null);
                }

            }
            else
            {
                Debug.LogError($"[TableSO] Cannot find dataList field in {tableSOType.Name}");
            }
        }
    }
}
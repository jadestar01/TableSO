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
        [MenuItem("TableSO/Load CSV Data to TableSO")]
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
                sos.Add(LoadCSVDataToTableSO(csvPath, tableOutputPath));
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
                tableCenter.ClearRegisteredTables();
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
            Debug.Log($"[TableSO] {tables.Count} tables automatically registered to TableCenter");
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
                
                // Set data to TableSO's dataList field
                SetTableSOData(tableSO, dataList);
                
                EditorUtility.SetDirty(tableSO);
                Debug.Log($"[TableSO] {fileName}.csv data loaded to {fileName}TableSO. Data count: {dataList.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error loading {fileName} CSV: {e.Message}");
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
                return dataList;
            }

            // Find constructor
            Type[] constructorTypes = new Type[fieldTypes.Length];
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                constructorTypes[i] = GetTypeFromString(fieldTypes[i]);
            }
            
            ConstructorInfo constructor = dataType.GetConstructor(constructorTypes);
            if (constructor == null)
            {
                Debug.LogError($"[TableSO] Cannot find constructor for {className}");
                return dataList;
            }

            // Process data rows (starting from row 3)
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseCSVLine(lines[i]);
                
                if (values.Length != fieldNames.Length)
                {
                    Debug.LogWarning($"[TableSO] {className}.csv row {i+1}: Field count mismatch");
                    continue;
                }

                object[] constructorArgs = new object[values.Length];
                
                for (int j = 0; j < values.Length; j++)
                {
                    constructorArgs[j] = ConvertValue(values[j], fieldTypes[j]);
                }
                
                object dataInstance = constructor.Invoke(constructorArgs);
                dataList.Add(dataInstance);
            }
            
            return dataList;
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
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType($"Table.{typeName}");
                if (type != null) return type;
            }
            return null;
        }

        private static Type FindDataClassType(string className)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType($"TableData.{className}");
                if (type != null) return type;
            }
            return null;
        }

        private static Type GetTypeFromString(string typeString)
        {
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
                    // Handle enum or custom types
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        Type type = assembly.GetType(typeString);
                        if (type != null) return type;
                        
                        // Find without namespace
                        foreach (Type assemblyType in assembly.GetTypes())
                        {
                            if (assemblyType.Name == typeString)
                            {
                                return assemblyType;
                            }
                        }
                    }
                    return typeof(string); // Default value
            }
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
                
                // Set isUpdated flag
                FieldInfo isUpdatedField = currentType?.GetField("isUpdated", BindingFlags.Public | BindingFlags.Instance);
                if (isUpdatedField != null)
                {
                    isUpdatedField.SetValue(tableSO, true);
                }
            }
            else
            {
                Debug.LogError($"[TableSO] Cannot find dataList field in {tableSOType.Name}");
            }
        }
    }
}
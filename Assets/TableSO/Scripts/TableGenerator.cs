using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace TableSO.Scripts.Generator
{
    public class TableGenerator : Editor
    {
        [MenuItem("TableSO/Generate All Tables")]
        public static void GenerateAllTables()
        {
            if (!Directory.Exists(FilePath.CSV_PATH))
            {
                Debug.LogError($"[TableSO] Cannot find CSV path: {FilePath.CSV_PATH}");
                return;
            }

            string[] csvFiles = Directory.GetFiles(FilePath.CSV_PATH, "*.csv");
            
            if (csvFiles.Length == 0)
            {
                Debug.LogWarning("[TableSO] Cannot find CSV files");
                return;
            }

            foreach (string csvPath in csvFiles)
            {
                GenerateTableFromCSV(csvPath);
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[TableSO] Generated <{csvFiles.Length}> tables");
        }

        public static void GenerateTableFromCSV(string csvPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(csvPath);
            
            try
            {
                string[] lines = File.ReadAllLines(csvPath);
                
                if (lines.Length < 2)
                {
                    Debug.LogError($"[TableSO] {fileName}.csv requires correct format: row 1 for variable names, row 2 for type names");
                    return;
                }

                // CSV parsing
                string[] fieldNames = ParseCSVLine(lines[0]);
                string[] fieldTypes = ParseCSVLine(lines[1]);
                
                if (fieldNames.Length != fieldTypes.Length)
                {
                    Debug.LogError($"[TableSO] {fileName}.csv: Variable name count and type count do not match");
                    return;
                }

                // Validate first field (ID field)
                if (!ValidateIDField(fieldNames[0], fieldTypes[0], fileName))
                    return;

                // Validate all field types
                if (!ValidateFieldTypes(fieldTypes, fileName))
                    return;

                // Generate class
                GenerateDataClass(fileName, fieldNames, fieldTypes);
                
                // Generate TableSO
                GenerateTableSO(fileName, fieldTypes[0], fileName);
                
                Debug.Log($"[TableSO] {fileName} table generated successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error processing {fileName}.csv: {e.Message}");
            }
        }

        private static string[] ParseCSVLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();
            
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

        private static bool ValidateIDField(string idFieldName, string idFieldType, string fileName)
        {
            if (!idFieldName.Equals("ID", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[TableSO] {fileName}.csv: First field must be 'ID'. Current: {idFieldName}");
                return false;
            }

            string normalizedType = idFieldType.ToLower().Trim();
            
            // Check basic types
            if (normalizedType == "int" || normalizedType == "string")
            {
                return true;
            }
            
            // Check if enum type
            if (IsEnumType(idFieldType))
            {
                if (ValidateEnumType(idFieldType))
                {
                    return true;
                }
                else
                {
                    Debug.LogError($"[TableSO] {fileName}.csv: Cannot find enum type '{idFieldType}' for ID field");
                    return false;
                }
            }

            Debug.LogError($"[TableSO] {fileName}.csv: ID field must be int, string, or enum type. Current: {idFieldType}");
            return false;
        }

        private static bool ValidateFieldTypes(string[] fieldTypes, string fileName)
        {
            for (int i = 0; i < fieldTypes.Length; i++)
            {
                string fieldType = fieldTypes[i].Trim();
                
                // Check if array type
                if (IsArrayType(fieldType))
                {
                    string elementType = GetArrayElementType(fieldType);
                    
                    // Check enum array type existence
                    if (IsEnumType(elementType))
                    {
                        if (!ValidateEnumType(elementType))
                        {
                            Debug.LogError($"[TableSO] {fileName}.csv: Cannot find enum type '{elementType}' (field {i + 1})");
                            return false;
                        }
                    }
                    // Check basic array type
                    else if (!IsValidBasicType(elementType))
                    {
                        Debug.LogError($"[TableSO] {fileName}.csv: Unsupported array element type: {elementType} (field {i + 1})");
                        return false;
                    }
                }
                // Single type case
                else if (IsEnumType(fieldType))
                {
                    if (!ValidateEnumType(fieldType))
                    {
                        Debug.LogError($"[TableSO] {fileName}.csv: Cannot find enum type '{fieldType}' (field {i + 1})");
                        return false;
                    }
                }
                else if (!IsValidBasicType(fieldType))
                {
                    Debug.LogError($"[TableSO] {fileName}.csv: Unsupported type: {fieldType} (field {i + 1})");
                    return false;
                }
            }
            
            return true;
        }

        private static bool IsArrayType(string type)
        {
            return type.EndsWith("[]");
        }

        private static string GetArrayElementType(string arrayType)
        {
            return arrayType.Substring(0, arrayType.Length - 2).Trim();
        }

        private static bool IsEnumType(string type)
        {
            // If not a basic type and presumed to be custom type
            string normalizedType = type.ToLower().Trim();
            return !IsValidBasicType(normalizedType) && !string.IsNullOrEmpty(type);
        }

        private static bool IsValidBasicType(string type)
        {
            string normalizedType = type.ToLower().Trim();
            return normalizedType == "int" || normalizedType == "float" || 
                   normalizedType == "string" || normalizedType == "bool" || 
                   normalizedType == "double";
        }

        // **수정된 부분: 더 정확한 enum 타입 검증**
        private static bool ValidateEnumType(string enumTypeName)
        {
            // 사용자 정의 enum만 찾기 (Unity 내부 타입 제외)
            var userDefinedEnums = FindUserDefinedEnumTypes(enumTypeName);
            
            if (userDefinedEnums.Count > 0)
            {
                if (userDefinedEnums.Count == 1)
                {
                    var foundEnum = userDefinedEnums[0];
                    Debug.Log($"[TableSO] Enum type '{enumTypeName}' confirmed (full name: {foundEnum.FullName})");
                    return true;
                }
                else
                {
                    // 여러 개의 enum이 발견된 경우 사용자가 선택하도록
                    string enumList = string.Join("\n", userDefinedEnums.Select(e => $"- {e.FullName}"));
                    
                    bool continueAnyway = EditorUtility.DisplayDialog(
                        "Multiple Enum Types Found",
                        $"Multiple enum types named '{enumTypeName}' found:\n\n{enumList}\n\n" +
                        "Do you want to use the first one and continue?",
                        "Yes",
                        "No");
                    
                    if (continueAnyway)
                    {
                        Debug.Log($"[TableSO] Using enum type: {userDefinedEnums[0].FullName}");
                        return true;
                    }
                    else
                    {
                        Debug.Log("[TableSO] User cancelled generation due to multiple enum types");
                        return false;
                    }
                }
            }
            
            // enum을 찾지 못한 경우 사용자가 계속 진행할지 선택
            Debug.LogWarning($"[TableSO] Cannot find user-defined enum type '{enumTypeName}' in current assemblies");
            
            bool continueWithoutEnum = EditorUtility.DisplayDialog(
                "Enum Type Not Found", 
                $"Cannot find enum type '{enumTypeName}'.\n\n" +
                "This might be because:\n" +
                "- The enum hasn't been created yet\n" +
                "- The enum is in a different namespace\n" +
                "- There's a typo in the enum name\n\n" +
                "Do you want to continue anyway?\n" +
                "(The code will be generated assuming the enum exists)",
                "Continue", 
                "Cancel");
                
            if (continueWithoutEnum)
            {
                Debug.Log($"[TableSO] User choice: Continue with enum type '{enumTypeName}' assumption");
                return true;
            }
            else
            {
                Debug.Log("[TableSO] User choice: Cancel generation");
                return false;
            }
        }

        // **새로 추가된 메서드: 사용자 정의 enum 타입만 찾기**
        private static List<Type> FindUserDefinedEnumTypes(string enumTypeName)
        {
            List<Type> userDefinedEnums = new List<Type>();
            
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

                    // 어셈블리의 모든 타입 검사
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsEnum && type.Name == enumTypeName)
                        {
                            // 추가 필터링: 중첩 타입이거나 내부 타입 제외
                            if (!type.IsNested && type.IsPublic)
                            {
                                userDefinedEnums.Add(type);
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
            
            return userDefinedEnums;
        }

        private static void GenerateDataClass(string className, string[] fieldNames, string[] fieldTypes)
        {
            StringBuilder classCode = new StringBuilder();
            
            classCode.AppendLine("using System;");
            classCode.AppendLine("using System.Collections.Generic;");
            classCode.AppendLine("using UnityEngine;");
            classCode.AppendLine();
            classCode.AppendLine("/// <summary>");
            classCode.AppendLine("/// Made by TableSO TableGenerator");
            classCode.AppendLine("/// </summary>");
            classCode.AppendLine();
            classCode.AppendLine("namespace TableData");
            classCode.AppendLine("{");
            classCode.AppendLine("    [System.Serializable]");
            classCode.AppendLine($"    public class {className} : IIdentifiable<{ConvertToValidType(fieldTypes[0])}>");
            classCode.AppendLine("    {");
            
            // Generate properties
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldType = ConvertToValidType(fieldTypes[i]);
                string fieldName = fieldNames[i];
                
                classCode.AppendLine($"        [field: SerializeField] public {fieldType} {fieldName} {{ get; internal set; }}");
                classCode.AppendLine();
            }
            
            // Constructor
            classCode.AppendLine($"        public {className}({string.Join(", ", GenerateConstructorParams(fieldNames, fieldTypes))})");
            classCode.AppendLine("        {");
            
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string paramName = fieldNames[i];
                classCode.AppendLine($"            this.{paramName} = {paramName};");
            }
            
            classCode.AppendLine("        }");
            classCode.AppendLine("    }");
            classCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.DATA_CLASS_PATH);
            string classFilePath = Path.Combine(FilePath.DATA_CLASS_PATH, $"{className}.cs");
            File.WriteAllText(classFilePath, classCode.ToString());
        }
        
        private static string[] GenerateConstructorParams(string[] fieldNames, string[] fieldTypes)
        {
            string[] parameters = new string[fieldNames.Length];
            
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string paramType = ConvertToValidType(fieldTypes[i]);
                string paramName = fieldNames[i];
                parameters[i] = $"{paramType} {paramName}";
            }
            
            return parameters;
        }

        private static void GenerateTableSO(string className, string idType, string fileName)
        {
            StringBuilder tableCode = new StringBuilder();
            
            tableCode.AppendLine("using UnityEngine;");
            tableCode.AppendLine("using TableData;");
            tableCode.AppendLine();
            tableCode.AppendLine("namespace Table");
            tableCode.AppendLine("{");
            tableCode.AppendLine($"    public class {className}TableSO : TableSO.Scripts.TableSO<{ConvertToValidType(idType)}, TableData.{className}>");
            tableCode.AppendLine("    {");
            tableCode.AppendLine($"        public string fileName = \"{fileName}\";");
            tableCode.AppendLine("    }");
            tableCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.TABLE_CLASS_PATH);
            string tableFilePath = Path.Combine(FilePath.TABLE_CLASS_PATH, $"{className}TableSO.cs");
            if (!File.Exists(tableFilePath))
                File.WriteAllText(tableFilePath, tableCode.ToString());
        }

        private static string ConvertToValidType(string csvType)
        {
            string normalizedType = csvType.ToLower().Trim();
            
            // Handle array types
            if (csvType.EndsWith("[]"))
            {
                string elementType = csvType.Substring(0, csvType.Length - 2).Trim();
                string convertedElementType = ConvertToValidType(elementType);
                return $"{convertedElementType}[]";
            }
            
            switch (normalizedType)
            {
                case "int":
                    return "int";
                case "float":
                    return "float";
                case "string":
                    return "string";
                case "bool":
                    return "bool";
                case "double":
                    return "double";
                default:
                    // Return as-is for enum or custom types
                    return csvType.Trim();
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        [MenuItem("TableSO/Generate From Selected CSV")]
        public static void GenerateFromSelectedCSV()
        {
            string[] guids = Selection.assetGUIDs;
            
            if (guids.Length == 0)
            {
                Debug.LogWarning("[TableSO] Please select CSV files");
                return;
            }

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (Path.GetExtension(assetPath).ToLower() == ".csv")
                {
                    GenerateTableFromCSV(assetPath);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}
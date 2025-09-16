#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;

namespace TableSO.Scripts.Generator
{
    public class MergeTableGenerator
    {
        // 키 타입을 받도록 메서드 시그니처 수정
        public static void GenerateMergeTable(string tableName, List<ScriptableObject> referencedTables, string keyType, bool autoRegister)
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    EditorUtility.DisplayDialog("Error", "Table name cannot be empty", "OK");
                    return;
                }

                if (referencedTables == null || referencedTables.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "At least one referenced table must be selected", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(keyType))
                {
                    EditorUtility.DisplayDialog("Error", "Key type cannot be empty", "OK");
                    return;
                }

                // Generate RefData class
                GenerateMergeDataClass(tableName, keyType, referencedTables);
                
                // Generate MergeTableSO class
                GenerateMergeTableSOClass(tableName, keyType, referencedTables);

                // Refresh to compile new scripts
                AssetDatabase.Refresh();
                
                Debug.Log($"[TableSO] RefTable '{tableName}' generated successfully with key type '{keyType}'");
                EditorUtility.DisplayDialog("Success", $"RefTable '{tableName}' generated successfully!", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating RefTable: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate RefTable:\n{e.Message}", "OK");
            }
        }
        
        private static void GenerateMergeDataClass(string className, string keyType, List<ScriptableObject> referencedTables)
        {
            StringBuilder classCode = new StringBuilder();
            
            classCode.AppendLine("using System;");
            classCode.AppendLine("using System.Collections.Generic;");
            classCode.AppendLine("using UnityEngine;");
            classCode.AppendLine("using TableSO.Scripts;");
            classCode.AppendLine();
            classCode.AppendLine("/// <summary>");
            classCode.AppendLine("/// Merge Data Class - Made by TableSO MergeTableGenerator");
            classCode.AppendLine($"/// Key Type: {keyType}");
            classCode.AppendLine("/// </summary>");
            classCode.AppendLine();
            classCode.AppendLine("namespace TableData");
            classCode.AppendLine("{");
            classCode.AppendLine("    [System.Serializable]");
            classCode.AppendLine($"    public class {className} : IIdentifiable<{keyType}>");
            classCode.AppendLine("    {");
            classCode.AppendLine($"        [field: SerializeField] public {keyType} ID {{ get; internal set; }}");
            
            // 참조된 테이블들의 데이터를 위한 필드들 추가
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                
                classCode.AppendLine($"        /// <summary>");
                classCode.AppendLine($"        /// Merge to {tableTypeName} data");
                classCode.AppendLine($"        /// </summary>");
                classCode.AppendLine($"        [field: SerializeField] public string {propertyName}ID {{ get; set; }}");
            }
            
            // 커스텀 데이터 필드들을 위한 공간
            classCode.AppendLine();
            classCode.AppendLine("        // Add your custom fields here");
            classCode.AppendLine("        // Example:");
            classCode.AppendLine("        // [field: SerializeField] public int CustomValue { get; set; }");
            classCode.AppendLine("        // [field: SerializeField] public string Description { get; set; }");
            
            classCode.AppendLine("    }");
            classCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.DATA_CLASS_PATH);
            string classFilePath = Path.Combine(FilePath.DATA_CLASS_PATH, $"{className}.cs");
            File.WriteAllText(classFilePath, classCode.ToString());
        }

        private static void GenerateMergeTableSOClass(string className, string keyType, List<ScriptableObject> referencedTables)
        {
            StringBuilder tableCode = new StringBuilder();
            
            tableCode.AppendLine("using UnityEngine;");
            tableCode.AppendLine("using TableData;");
            tableCode.AppendLine("using System.Collections.Generic;");
            tableCode.AppendLine("using System.Linq;");
            tableCode.AppendLine("using System;");
            tableCode.AppendLine("using TableSO.Scripts;");
            tableCode.AppendLine();
            tableCode.AppendLine("/// <summary>");
            tableCode.AppendLine($"/// Merge Table - Made by TableSO MergeTableGenerator");
            tableCode.AppendLine($"/// Key Type: {keyType}");
            tableCode.AppendLine($"/// Referenced Tables: {string.Join(", ", referencedTables.Where(t => t != null).Select(t => t.GetType().Name))}");
            tableCode.AppendLine("/// </summary>");
            tableCode.AppendLine();
            tableCode.AppendLine("namespace Table");
            tableCode.AppendLine("{");
            tableCode.AppendLine($"    public class {className}MergeTableSO : TableSO.Scripts.MergeTableSO<{keyType}, TableData.{className}>");
            tableCode.AppendLine("    {");
            tableCode.AppendLine($"        public string fileName => \"{className}MergeTableSO\";");
            
            // 참조된 테이블들을 위한 private 필드들
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                
                tableCode.AppendLine($"        [SerializeField] private {tableTypeName} {propertyName}Table;");
            }
            
            tableCode.AppendLine();
            
            // refTableTypes 속성 구현
            tableCode.AppendLine($"        public List<Type> refTableTypes {{ get; set; }} = new List<Type>()");
            tableCode.AppendLine($"        {{");
            
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                tableCode.AppendLine($"            typeof({tableTypeName}),");
            }
            tableCode.AppendLine($"        }};");
            tableCode.AppendLine();
            tableCode.AppendLine($"        public override void UpdateData()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            // TODO: Implement UpdateDatalogic");
            tableCode.AppendLine("        }");
            tableCode.AppendLine();
            tableCode.AppendLine($"        public override TableData.{className} GetData({keyType} key)");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            // TODO: Implement GetData logic");
            tableCode.AppendLine("            // This should return the RefData that matches the key");
            tableCode.AppendLine("            // You may want to create data dynamically based on referenced tables");
            tableCode.AppendLine($"            return base.GetData(key);");
            tableCode.AppendLine("        }");
            
            tableCode.AppendLine("    }");
            tableCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.TABLE_CLASS_PATH);
            string tableFilePath = Path.Combine(FilePath.TABLE_CLASS_PATH, $"{className}MergeTableSO.cs");
            File.WriteAllText(tableFilePath, tableCode.ToString());
        }
        
        private static string GetTablePropertyName(string tableTypeName)
        {
            // Remove "TableSO" suffix and return clean name
            if (tableTypeName.EndsWith("TableSO"))
            {
                return tableTypeName.Substring(0, tableTypeName.Length - 7);
            }
            return tableTypeName;
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static List<ScriptableObject> GetAllAvailableTables()
        {
            List<ScriptableObject> tables = new List<ScriptableObject>();
            
            // Find all ScriptableObjects that inherit from TableSO or AssetTableSO
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                
                if (asset != null && IsTableSO(asset))
                {
                    tables.Add(asset);
                }
            }
            
            return tables.OrderBy(t => t.name).ToList();
        }

        private static bool IsTableSO(ScriptableObject obj)
        {
            Type objType = obj.GetType();
            
            // Check if it implements ITableType
            if (typeof(ITableType).IsAssignableFrom(objType))
            {
                return true;
            }
            
            // Check if it inherits from TableSO (generic)
            Type currentType = objType;
            while (currentType != null && currentType != typeof(ScriptableObject))
            {
                if (currentType.IsGenericType)
                {
                    Type genericTypeDef = currentType.GetGenericTypeDefinition();
                    if (genericTypeDef.Name.StartsWith("TableSO") || 
                        genericTypeDef.Name.StartsWith("AssetTableSO") ||
                        genericTypeDef.Name.StartsWith("MergeTableSO"))
                    {
                        return true;
                    }
                }
                currentType = currentType.BaseType;
            }
            
            return false;
        }
        public static bool ValidateTableReferences(List<ScriptableObject> tables)
        {
            foreach (var table in tables)
            {
                if (table == null)
                {
                    Debug.LogWarning("[MergeTableGenerator] Null table reference found");
                    return false;
                }
                
                if (!IsTableSO(table))
                {
                    Debug.LogWarning($"[MergeTableGenerator] {table.name} is not a valid TableSO");
                    return false;
                }
            }
            
            return true;
        }
    }
}
#endif
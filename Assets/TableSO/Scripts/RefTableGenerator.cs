using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TableSO.FileUtility;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace TableSO.Scripts.Generator
{
    public class RefTableGenerator : EditorWindow
    {
        [MenuItem("TableSO/Reference Table Generator")]
        public static void ShowWindow()
        {
            GetWindow<RefTableGenerator>("Reference Table Generator");
        }

        public static void GenerateRefTable(string tableName, List<ScriptableObject> referencedTables, 
            List<CustomOperation> customOperations, bool autoRegister)
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

                // Generate RefData class
                GenerateRefDataClass(tableName, referencedTables, customOperations);
                
                // Generate RefTableSO class
                GenerateRefTableSOClass(tableName, referencedTables, customOperations);

                // Refresh to compile new scripts
                AssetDatabase.Refresh();
                
                Debug.Log($"[TableSO] RefTable '{tableName}' generated successfully");
                EditorUtility.DisplayDialog("Success", $"RefTable '{tableName}' generated successfully!", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] Error generating RefTable: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate RefTable:\n{e.Message}", "OK");
            }
        }
        
        private static void GenerateRefDataClass(string className, List<ScriptableObject> referencedTables, 
            List<CustomOperation> customOperations)
        {
            StringBuilder classCode = new StringBuilder();
            
            classCode.AppendLine("using System;");
            classCode.AppendLine("using System.Collections.Generic;");
            classCode.AppendLine("using UnityEngine;");
            classCode.AppendLine("using TableSO.Scripts;");
            classCode.AppendLine();
            classCode.AppendLine("/// <summary>");
            classCode.AppendLine("/// Reference Data Class - Made by TableSO RefTableGenerator");
            classCode.AppendLine("/// </summary>");
            classCode.AppendLine();
            classCode.AppendLine("namespace TableData");
            classCode.AppendLine("{");
            classCode.AppendLine("    [System.Serializable]");
            classCode.AppendLine($"    public class {className} : IIdentifiable<string>");
            classCode.AppendLine("    {");
            classCode.AppendLine("        [field: SerializeField] public string ID { get; internal set; }");
            classCode.AppendLine();
            
            // Generate reference properties for each referenced table
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                
                classCode.AppendLine($"        [field: SerializeField] public string {propertyName}ID {{ get; internal set; }}");
            }
            
            classCode.AppendLine();
            
            // Generate custom operation results storage
            foreach (var operation in customOperations)
            {
                if (string.IsNullOrEmpty(operation.name)) continue;
                
                classCode.AppendLine($"        [field: SerializeField] public {operation.returnType} {operation.name}Result {{ get; internal set; }}");
            }
            
            classCode.AppendLine();
            
            // Constructor
            List<string> constructorParams = new List<string> { "string id" };
            
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                constructorParams.Add($"string {propertyName.ToLower()}ID = \"\"");
            }
            
            foreach (var operation in customOperations)
            {
                if (string.IsNullOrEmpty(operation.name)) continue;
                constructorParams.Add($"{operation.returnType} {operation.name.ToLower()}Result = default");
            }
            
            classCode.AppendLine($"        public {className}({string.Join(", ", constructorParams)})");
            classCode.AppendLine("        {");
            classCode.AppendLine("            this.ID = id;");
            
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                classCode.AppendLine($"            this.{propertyName}ID = {propertyName.ToLower()}ID;");
            }
            
            foreach (var operation in customOperations)
            {
                if (string.IsNullOrEmpty(operation.name)) continue;
                classCode.AppendLine($"            this.{operation.name}Result = {operation.name.ToLower()}Result;");
            }
            
            classCode.AppendLine("        }");
            classCode.AppendLine("    }");
            classCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.DATA_CLASS_PATH);
            string classFilePath = Path.Combine(FilePath.DATA_CLASS_PATH, $"{className}.cs");
            File.WriteAllText(classFilePath, classCode.ToString());
        }

        private static void GenerateRefTableSOClass(string className, List<ScriptableObject> referencedTables, 
            List<CustomOperation> customOperations)
        {
            StringBuilder tableCode = new StringBuilder();
            
            tableCode.AppendLine("using UnityEngine;");
            tableCode.AppendLine("using TableData;");
            tableCode.AppendLine("using System.Collections.Generic;");
            tableCode.AppendLine("using System.Linq;");
            tableCode.AppendLine("using System;");
            tableCode.AppendLine("using TableSO.Scripts;");
            tableCode.AppendLine();
            tableCode.AppendLine("namespace Table");
            tableCode.AppendLine("{");
            tableCode.AppendLine($"    public class {className}RefTableSO : TableSO.Scripts.RefTableSO<string, TableData.{className}>, IReferencable");
            tableCode.AppendLine("    {");
            tableCode.AppendLine($"        public string fileName => \"{className}RefTableSO\";");
            
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                string propertyName = GetTablePropertyName(tableTypeName);
                
                tableCode.AppendLine($"        [SerializeField] private {tableTypeName} {propertyName}Table;");
            }
            
            tableCode.AppendLine($"        public List<Type> refTableTypes {{ get; set; }} = new List<Type>()");
            tableCode.AppendLine($"        {{");
            
            // Generate reference table getters
            foreach (var table in referencedTables)
            {
                if (table == null) continue;
                
                string tableTypeName = table.GetType().Name;
                
                tableCode.AppendLine($"            typeof({tableTypeName}),");
            }
            tableCode.AppendLine($"        }};");

            
            // Generate custom operations
            foreach (var operation in customOperations)
            {
                if (string.IsNullOrEmpty(operation.name)) continue;
                
                tableCode.AppendLine($"        /// <summary>");
                tableCode.AppendLine($"        /// Custom operation: {operation.description}");
                tableCode.AppendLine($"        /// </summary>");
                tableCode.AppendLine($"        public {operation.returnType} {operation.name}({operation.parameters})");
                tableCode.AppendLine("        {");
                tableCode.AppendLine($"            // TODO: Implement {operation.name} logic here");
                tableCode.AppendLine($"            // You can access referenced tables using Get[TableName]() methods");
                tableCode.AppendLine();
                
                // Generate sample code based on operation type
                if (operation.returnType.Contains("List") || operation.returnType.EndsWith("[]"))
                {
                    tableCode.AppendLine($"            var result = new List<{ExtractGenericType(operation.returnType)}>();");
                    tableCode.AppendLine("            // Add your custom logic here");
                    tableCode.AppendLine("            return result" + (operation.returnType.EndsWith("[]") ? ".ToArray()" : "") + ";");
                }
                else if (operation.returnType == "bool")
                {
                    tableCode.AppendLine("            // Add your custom logic here");
                    tableCode.AppendLine("            return false; // Change this to your actual logic");
                }
                else if (operation.returnType == "int" || operation.returnType == "float")
                {
                    tableCode.AppendLine("            // Add your custom logic here");
                    tableCode.AppendLine("            return 0; // Change this to your actual logic");
                }
                else if (operation.returnType == "string")
                {
                    tableCode.AppendLine("            // Add your custom logic here");
                    tableCode.AppendLine("            return string.Empty; // Change this to your actual logic");
                }
                else
                {
                    tableCode.AppendLine("            // Add your custom logic here");
                    tableCode.AppendLine($"            return default({operation.returnType}); // Change this to your actual logic");
                }
                
                tableCode.AppendLine("        }");
                tableCode.AppendLine();
            }
            
            // Override ExecuteCustomOperation
            if (customOperations.Count > 0)
            {
                tableCode.AppendLine("        public override TResult ExecuteCustomOperation<TResult>(string operationName, params object[] parameters)");
                tableCode.AppendLine("        {");
                tableCode.AppendLine("            switch (operationName)");
                tableCode.AppendLine("            {");
                
                foreach (var operation in customOperations)
                {
                    if (string.IsNullOrEmpty(operation.name)) continue;
                    
                    tableCode.AppendLine($"                case \"{operation.name}\":");
                    tableCode.AppendLine($"                    return (TResult)(object){operation.name}({GenerateParameterCast(operation.parameters)});");
                }
                
                tableCode.AppendLine("                default:");
                tableCode.AppendLine("                    return base.ExecuteCustomOperation<TResult>(operationName, parameters);");
                tableCode.AppendLine("            }");
                tableCode.AppendLine("        }");
                tableCode.AppendLine();
            }
            
            // Override refresh logic
            tableCode.AppendLine("        protected override void OnRefreshFromReferencedTables()");
            tableCode.AppendLine("        {");
            tableCode.AppendLine("            // TODO: Implement custom refresh logic");
            tableCode.AppendLine("            // This method is called when RefreshFromReferencedTables() is invoked");
            tableCode.AppendLine("            // You can update your dataList based on referenced tables here");
            tableCode.AppendLine("            ");
            tableCode.AppendLine("            base.OnRefreshFromReferencedTables();");
            tableCode.AppendLine("        }");
            tableCode.AppendLine("    }");
            tableCode.AppendLine("}");

            // Save file
            EnsureDirectoryExists(FilePath.TABLE_CLASS_PATH);
            string tableFilePath = Path.Combine(FilePath.TABLE_CLASS_PATH, $"{className}RefTableSO.cs");
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

        private static string ExtractGenericType(string returnType)
        {
            if (returnType.StartsWith("List<") && returnType.EndsWith(">"))
            {
                return returnType.Substring(5, returnType.Length - 6);
            }
            if (returnType.EndsWith("[]"))
            {
                return returnType.Substring(0, returnType.Length - 2);
            }
            return returnType;
        }

        private static string GenerateParameterCast(string parameters)
        {
            if (string.IsNullOrEmpty(parameters)) return "";
            
            var paramList = parameters.Split(',').Select(p => p.Trim()).ToList();
            var castParams = new List<string>();
            
            for (int i = 0; i < paramList.Count; i++)
            {
                var parts = paramList[i].Split(' ');
                if (parts.Length >= 2)
                {
                    string type = parts[0];
                    castParams.Add($"({type})parameters[{i}]");
                }
            }
            
            return string.Join(", ", castParams);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Get all available TableSO and AssetTableSO instances in the project
        /// </summary>
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
                        genericTypeDef.Name.StartsWith("RefTableSO"))
                    {
                        return true;
                    }
                }
                currentType = currentType.BaseType;
            }
            
            return false;
        }

        /// <summary>
        /// Get table information for display
        /// </summary>
        public static string GetTableInfo(ScriptableObject table)
        {
            if (table == null) return "Unknown";
            
            string tableName = table.name;
            string tableType = "Unknown";
            
            if (table is ITableType tableTypeInterface)
            {
                tableType = tableTypeInterface.tableType.ToString();
            }
            
            return $"{tableName} ({tableType})";
        }

        /// <summary>
        /// Validate that table references are compatible
        /// </summary>
        public static bool ValidateTableReferences(List<ScriptableObject> tables)
        {
            foreach (var table in tables)
            {
                if (table == null)
                {
                    Debug.LogWarning("[RefTableGenerator] Null table reference found");
                    return false;
                }
                
                if (!IsTableSO(table))
                {
                    Debug.LogWarning($"[RefTableGenerator] {table.name} is not a valid TableSO");
                    return false;
                }
            }
            
            return true;
        }
    }

    /// <summary>
    /// Data structure for custom operations in RefTable
    /// </summary>
    [System.Serializable]
    public class CustomOperation
    {
        public string name;
        public string description;
        public string returnType;
        public string parameters;
        
        public CustomOperation(string name, string description, string returnType, string parameters = "")
        {
            this.name = name;
            this.description = description;
            this.returnType = returnType;
            this.parameters = parameters;
        }
    }
}
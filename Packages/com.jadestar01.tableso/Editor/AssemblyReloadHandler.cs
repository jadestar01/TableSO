using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TableSO.Scripts;
using System.IO;
using TableSO.Scripts.Editor;
using TableSO.Scripts.Generator;

[InitializeOnLoad]
public static class AssemblyReloadHandler
{
    private const string GENERATED_TABLES_FOLDER = "Assets/TableSO/Table";
    private const string GENERATED_CODE_FOLDER = "Assets/TableSO/Scripts/TableClass";
    
#if UNITY_EDITOR
    static AssemblyReloadHandler()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private static void OnBeforeAssemblyReload()
    {
        SaveCurrentTableState();
    }

    private static void OnAfterAssemblyReload()
    {
        bool autoReload = EditorPrefs.GetBool("AutoReload");
        Debug.Log($"[TableSO] Auto Reload: {autoReload}");
        if (!autoReload)
            return;
        Debug.Log("[TableSO] Try to find Tables..");

        // Wait with a longer delay until the assembly is fully loaded
        EditorApplication.delayCall += () => { EditorApplication.delayCall += () => { InitializeGeneratedTables(); }; };
    }

    private static void SaveCurrentTableState()
    {
        var tableCenter = FindTableCenter();
        if (tableCenter != null)
        {
            EditorUtility.SetDirty(tableCenter);
            AssetDatabase.SaveAssets();
        }
    }
    public static void InitializeGeneratedTables()
    {
        try
        {
            // First, check the generated .cs files
            var generatedFiles = FindGeneratedTableFiles();

            TableSOEditor.CreateCsvAddressableGroup();

            if (generatedFiles.Count == 0)
            {
                return;
            }

            // Find types (try multiple ways)
            var tableSoTypes = FindAllTableSOTypes();

            var tableCenter = FindOrCreateTableCenter();
            if (tableCenter == null)
            {
                return;
            }

            int createdCount = 0;
            int registeredCount = 0;

            tableCenter.ClearRegisteredTables();

            foreach (var tableType in tableSoTypes)
            {
                try
                {
                    var assetCreated = CreateTableSOAssetIfNotExists(tableType);
                    if (assetCreated.created)
                    {
                        createdCount++;
                        Debug.Log($"[TableSO] New asset created: {tableType.Name}");
                    }

                    if (assetCreated.asset != null)
                    {
                        var registered = RegisterToTableCenter(tableCenter, assetCreated.asset);
                        if (registered)
                        {
                            registeredCount++;
                            Debug.Log($"[TableSO] Registered to TableCenter: {tableType.Name}");
                        }

                        if (assetCreated.asset is IUpdatable updatable)
                            updatable.UpdateData();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] Error while processing {tableType.Name}: {e.Message}");
                }
            }

            EditorApplication.delayCall += () => {
                AssignRefTableReferences(tableCenter, tableSoTypes);
                
                if (createdCount > 0 || registeredCount > 0)
                {
                    EditorUtility.SetDirty(tableCenter);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Error during initialization: {e.Message}");
        }
    }
    private static void AssignRefTableReferences(TableCenter tableCenter, List<Type> tableSoTypes)
    {
        try
        {
            foreach (var tableType in tableSoTypes)
            {
                try
                {
                    if (typeof(ICustomizable).IsAssignableFrom(tableType))
                    {
                        string assetName = tableType.Name;
                        string assetPath = Path.Combine(GENERATED_TABLES_FOLDER, $"{assetName}.asset");
                        var refTableAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                        if (refTableAsset != null && refTableAsset is ICustomizable referencable)
                        {
                            AssignReferencesToRefTable(tableCenter, refTableAsset, referencable);
                            
                            try
                            {
                                var refreshMethod = refTableAsset.GetType().GetMethod("RefreshFromReferencedTables");
                                if (refreshMethod != null)
                                {
                                    refreshMethod.Invoke(refTableAsset, null);
                                }
                            }
                            catch (Exception refreshEx)
                            {
                                Debug.LogWarning($"[TableSO] Error while refreshing RefTable {refTableAsset.name}: {refreshEx.Message}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] Error assigning references for RefTable {tableType.Name}: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Fatal error during RefTable reference assignment stage: {e.Message}");
        }
    }

    private static void AssignReferencesToRefTable(TableCenter tableCenter, ScriptableObject refTableAsset,
        ICustomizable consultable)
    {
        try
        {
            int assignedCount = 0;
            foreach (var type in consultable.refTableTypes)
            {
                foreach (var table in tableCenter.GetRegisteredTables())
                {
                    if (table.GetType() == type)
                    {
                        FieldInfo field = refTableAsset.GetType().GetField(GetTablePropertyName(type.Name),
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        if (field != null)
                        {
                            field.SetValue(refTableAsset, table);
                            assignedCount++;
                        }
                        
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(refTableAsset);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Error assigning references for RefTable {refTableAsset.name}: {e.Message}");
        }
    }

    private static string GetTablePropertyName(string str)
    {
        return str.Substring(0, str.Length - 2);
    }

    private static List<string> FindGeneratedTableFiles()
    {
        var files = new List<string>();

        if (!Directory.Exists(GENERATED_CODE_FOLDER))
        {
            Debug.LogWarning($"[TableSO] Generated folder does not exist: {GENERATED_CODE_FOLDER}");
            return files;
        }

        // Find .cs files
        var csFiles = Directory.GetFiles(GENERATED_CODE_FOLDER, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            string relativePath = file.Replace('\\', '/');
            if (relativePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }

            // Check file content for TableSO inheritance
            if (IsTableSOFile(file))
            {
                files.Add(relativePath);
            }
        }

        return files;
    }

    private static bool IsTableSOFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);

            // Check for TableSO inheritance pattern
            return content.Contains("TableSO<") ||
                   content.Contains("AssetTableSO<") ||
                   content.Contains(": TableSO") ||
                   content.Contains(": AssetTableSO");
        }
        catch
        {
            return false;
        }
    }

    private static List<Type> FindAllTableSOTypes()
    {
        var tableSoTypes = new List<Type>();

        try
        {
            // Collect expected type names based on generated files
            var generatedFiles = FindGeneratedTableFiles();
            var expectedTypeNames = new HashSet<string>();

            foreach (var file in generatedFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                expectedTypeNames.Add(fileName);
            }

            // Search only in Assembly-CSharp
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.StartsWith("Assembly-CSharp"))
                .ToList();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => !t.IsAbstract &&
                                    !t.IsGenericTypeDefinition &&
                                    IsGeneratedTableSOType(t, expectedTypeNames))
                        .ToList();

                    foreach (var type in types)
                    {
                        tableSoTypes.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Process only loadable types
                    if (ex.Types != null)
                    {
                        var loadableTypes = ex.Types.Where(t => t != null &&
                                                                !t.IsAbstract &&
                                                                !t.IsGenericTypeDefinition &&
                                                                IsGeneratedTableSOType(t, expectedTypeNames));
                        tableSoTypes.AddRange(loadableTypes);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TableSO] Error while processing assembly {assembly.FullName}: {ex.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Fatal error during type search: {e.Message}");
        }

        return tableSoTypes;
    }

    private static bool IsGeneratedTableSOType(Type type, HashSet<string> expectedTypeNames)
    {
        if (type == null) return false;

        try
        {
            // 1. Check if type name is in expected list (most accurate method)
            if (!expectedTypeNames.Contains(type.Name))
            {
                return false;
            }

            // 2. Check TableSO inheritance
            if (!IsTableSOType(type))
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TableSO] Error while checking type {type.Name}: {e.Message}");
        }

        return false;
    }

    private static bool IsTableSOType(Type type)
    {
        if (type == null) return false;

        try
        {
            // Check if inherits from TableSO<,> or AssetTableSO<>
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    var genericDef = baseType.GetGenericTypeDefinition();
                    if (genericDef.Name.StartsWith("TableSO") ||
                        genericDef.Name.StartsWith("AssetTableSO"))
                    {
                        return true;
                    }
                }

                baseType = baseType.BaseType;
            }

            // Check if implements ITableType interface
            if (type.GetInterfaces().Any(i => i.Name == "ITableType"))
            {
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TableSO] Error while checking type {type.Name}: {e.Message}");
        }

        return false;
    }

    private static (bool created, ScriptableObject asset) CreateTableSOAssetIfNotExists(Type tableType)
    {
        string assetName = tableType.Name;
        string assetPath = Path.Combine(GENERATED_TABLES_FOLDER, $"{assetName}.asset");

        // Check if it already exists
        var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (existingAsset != null)
        {
            return (false, existingAsset);
        }

        // Ensure folder exists
        EnsureDirectoryExists(GENERATED_TABLES_FOLDER);

        try
        {
            // Create ScriptableObject instance
            var instance = ScriptableObject.CreateInstance(tableType);
            if (instance == null)
            {
                Debug.LogError($"[TableSO] Failed to create instance of {tableType.Name}");
                return (false, null);
            }

            // Set name
            instance.name = assetName;

            // Save as Asset
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            return (true, instance);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Failed to create instance of {tableType.Name}: {e.Message}");
            return (false, null);
        }
    }

    private static bool RegisterToTableCenter(TableCenter tableCenter, ScriptableObject tableAsset)
    {
        if (tableCenter == null || tableAsset == null)
            return false;

        // Check if this table is already registered in TableCenter
        if (IsAlreadyRegistered(tableCenter, tableAsset))
        {
            return false;
        }

        try
        {
            // Check if it implements ITableType interface
            if (!(tableAsset is ITableType tableInterface))
            {
                Debug.LogWarning($"[TableSO] {tableAsset.name} does not implement ITableType.");

                // Force register as Asset type if it's AssetTableSO
                if (IsAssetTableType(tableAsset.GetType()))
                {
                    RegisterTableByType(tableCenter, tableAsset);
                    return true;
                }
                else
                {
                    RegisterTableByType(tableCenter, tableAsset);
                    return true;
                }
            }

            // Register to the correct list depending on TableType
            var tableType = tableInterface.tableType;
            RegisterTableByType(tableCenter, tableAsset);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Error while registering {tableAsset.name}: {e.Message}");
            return false;
        }
    }

    private static bool IsAssetTableType(Type type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().Name.Contains("AssetTableSO"))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static void RegisterTableByType(TableCenter tableCenter, ScriptableObject tableAsset)
    {
        tableCenter.RegisterTable(tableAsset);
    }

    private static bool IsAlreadyRegistered(TableCenter tableCenter, ScriptableObject tableAsset)
    {
        var tableCenterType = tableCenter.GetType();
        string[] listFieldNames = { "csvTables", "assetTables", "refTables" };

        foreach (var fieldName in listFieldNames)
        {
            var field = tableCenterType.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(tableCenter) as System.Collections.IList;
                if (list != null && list.Contains(tableAsset))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static TableCenter FindOrCreateTableCenter()
    {
        var tableCenter = FindTableCenter();
        if (tableCenter != null)
        {
            return tableCenter;
        }

        return CreateTableCenter();
    }

    private static TableCenter FindTableCenter()
    {
        string[] guids = AssetDatabase.FindAssets("t:TableCenter");

        if (guids.Length == 0)
        {
            return null;
        }

        string tableCenterPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var tableCenter = AssetDatabase.LoadAssetAtPath<TableCenter>(tableCenterPath);
        return tableCenter;
    }

    private static TableCenter CreateTableCenter()
    {
        try
        {
            var instance = ScriptableObject.CreateInstance<TableCenter>();

            EnsureDirectoryExists("Assets/TableSO");
            string assetPath = "Assets/TableSO/TableCenter.asset";

            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return instance;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] Error while creating TableCenter: {e.Message}");
            return null;
        }
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }
    }
#endif
}
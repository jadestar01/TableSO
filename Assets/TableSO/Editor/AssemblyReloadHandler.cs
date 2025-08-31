using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TableSO.Scripts;
using System.IO;
using TableSO.Scripts.Generator;

[InitializeOnLoad]
public static class AssemblyReloadHandler
{
    private const string GENERATED_TABLES_FOLDER = "Assets/TableSO/Table";
    private const string GENERATED_CODE_FOLDER = "Assets/TableSO/Scripts/TableClass";
    
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
        Debug.Log("[TableSO] Try to find Tables..");
        
        // 더 긴 딜레이로 어셈블리가 완전히 로드될 때까지 대기
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                InitializeGeneratedTables();
            };
        };
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

    private static void InitializeGeneratedTables()
    {
        try
        {
            // 먼저 생성된 .cs 파일들을 확인
            var generatedFiles = FindGeneratedTableFiles();
            
            if (generatedFiles.Count == 0)
            {
                return;
            }

            // 타입 찾기 (여러 방법 시도)
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
                        Debug.Log($"[TableSO] 새 에셋 생성: {tableType.Name}");
                    }

                    if (assetCreated.asset != null)
                    {
                        var registered = RegisterToTableCenter(tableCenter, assetCreated.asset);
                        if (registered)
                        {
                            registeredCount++;
                            Debug.Log($"[TableSO] TableCenter에 등록: {tableType.Name}");
                        }

                        if (assetCreated.asset is IUpdatable updatable)
                            updatable.UpdateData();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TableSO] {tableType.Name} 처리 중 오류: {e.Message}");
                }
            }

            if (createdCount > 0 || registeredCount > 0)
            {
                EditorUtility.SetDirty(tableCenter);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
            }
            else
            {
                Debug.Log("[TableSO] 처리할 새로운 테이블이 없습니다.");
            }
            
            CSVDataLoader.LoadAllCSVData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] 초기화 중 오류: {e.Message}");
        }
    }

    private static void Noe()
    {
        /*
        
        foreach (var asset in assets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                    
            var entry = settings.CreateOrMoveEntry(assetGUID, group, false, false);
            entry.address = GetAssetName(asset); // Use filename as address
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
*/
    }

    private static List<string> FindGeneratedTableFiles()
    {
        var files = new List<string>();
        
        if (!Directory.Exists(GENERATED_CODE_FOLDER))
        {
            Debug.LogWarning($"[TableSO] 생성 폴더가 존재하지 않습니다: {GENERATED_CODE_FOLDER}");
            return files;
        }

        // .cs 파일들 찾기
        var csFiles = Directory.GetFiles(GENERATED_CODE_FOLDER, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in csFiles)
        {
            string relativePath = file.Replace('\\', '/');
            if (relativePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            // 파일 내용을 확인해서 TableSO를 상속받는지 확인
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
            
            // TableSO 상속 패턴 확인
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
            // 먼저 생성된 파일들을 기반으로 타입 이름 수집
            var generatedFiles = FindGeneratedTableFiles();
            var expectedTypeNames = new HashSet<string>();
            
            foreach (var file in generatedFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                expectedTypeNames.Add(fileName);
            }
            
            // Assembly-CSharp에서만 검색
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
                    // 로드 가능한 타입들만 처리
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
                    Debug.LogWarning($"[TableSO] 어셈블리 {assembly.FullName} 처리 중 오류: {ex.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] 타입 검색 중 치명적 오류: {e.Message}");
        }

        return tableSoTypes;
    }

    private static bool IsGeneratedTableSOType(Type type, HashSet<string> expectedTypeNames)
    {
        if (type == null) return false;
        
        try
        {
            // 1. 예상되는 타입 이름 목록에 있는지 확인 (가장 정확한 방법)
            if (!expectedTypeNames.Contains(type.Name))
            {
                return false;
            }

            // 2. TableSO 상속 확인
            if (!IsTableSOType(type))
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
        }

        return false;
    }

    private static bool IsTableSOType(Type type)
    {
        if (type == null) return false;
        
        try
        {
            // TableSO<,> 또는 AssetTableSO<> 상속 확인
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

            // ITableType 인터페이스 구현 확인
            if (type.GetInterfaces().Any(i => i.Name == "ITableType"))
            {
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TableSO] 타입 {type.Name} 확인 중 오류: {e.Message}");
        }

        return false;
    }

    private static (bool created, ScriptableObject asset) CreateTableSOAssetIfNotExists(Type tableType)
    {
        string assetName = tableType.Name;
        string assetPath = Path.Combine(GENERATED_TABLES_FOLDER, $"{assetName}.asset");

        // 이미 존재하는지 확인
        var existingAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (existingAsset != null)
        {
            Debug.Log($"[TableSO] 기존 에셋 발견: {assetPath}");
            return (false, existingAsset);
        }

        // 폴더 생성
        EnsureDirectoryExists(GENERATED_TABLES_FOLDER);

        try
        {
            // ScriptableObject 인스턴스 생성
            var instance = ScriptableObject.CreateInstance(tableType);
            if (instance == null)
            {
                Debug.LogError($"[TableSO] {tableType.Name}의 인스턴스를 생성할 수 없습니다.");
                return (false, null);
            }

            // 이름 설정
            instance.name = assetName;

            // Asset으로 저장
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TableSO] ScriptableObject 생성 완료: {assetPath}");
            return (true, instance);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] {tableType.Name} 인스턴스 생성 실패: {e.Message}");
            return (false, null);
        }
    }

    private static bool RegisterToTableCenter(TableCenter tableCenter, ScriptableObject tableAsset)
    {
        if (tableCenter == null || tableAsset == null)
            return false;

        // TableCenter에 해당 테이블이 이미 등록되어 있는지 확인
        if (IsAlreadyRegistered(tableCenter, tableAsset))
        {
            Debug.Log($"[TableSO] {tableAsset.name}은 이미 등록되어 있습니다.");
            return false;
        }

        try
        {
            // ITableType 인터페이스를 구현하는지 확인
            if (!(tableAsset is ITableType tableInterface))
            {
                Debug.LogWarning($"[TableSO] {tableAsset.name}은 ITableType을 구현하지 않습니다.");
                
                // AssetTableSO인지 확인해서 강제로 Asset 타입으로 등록
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

            // TableType에 따라 적절한 리스트에 추가
            var tableType = tableInterface.tableType;
            RegisterTableByType(tableCenter, tableAsset);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] {tableAsset.name} 등록 중 오류: {e.Message}");
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
            Debug.Log("[TableSO] 기존 TableCenter를 찾을 수 없습니다.");
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

            Debug.Log($"[TableSO] TableCenter 생성 완료: {assetPath}");
            return instance;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] TableCenter 생성 실패: {e.Message}");
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

    // 수동으로 테이블 검색 및 등록을 실행할 수 있는 메뉴 항목
    [MenuItem("TableSO/Scan and Register Generated Tables")]
    public static void ManualScanAndRegister()
    {
        Debug.Log("[TableSO] 수동 테이블 스캔 및 등록 시작...");
        InitializeGeneratedTables();
    }

    // 디버그: 현재 어셈블리의 모든 타입 출력
    [MenuItem("TableSO/Debug/Print All Types")]
    public static void PrintAllTypes()
    {
        Debug.Log("[TableSO] 현재 도메인의 모든 TableSO 관련 타입:");
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName.Contains("Assembly-CSharp"))
            {
                Debug.Log($"어셈블리: {assembly.FullName}");
                
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.Namespace?.Contains("TableSO") == true || 
                                   t.Name.Contains("Table"))
                        .ToList();
                    
                    foreach (var type in types)
                    {
                        Debug.Log($"  - {type.FullName} (Abstract: {type.IsAbstract}, Generic: {type.IsGenericTypeDefinition})");
                        
                        if (type.BaseType != null)
                        {
                            Debug.Log($"    BaseType: {type.BaseType.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"타입 로드 실패: {e.Message}");
                }
            }
        }
    }

    // 생성된 파일 목록 출력
    [MenuItem("TableSO/Debug/Print Generated Files")]
    public static void PrintGeneratedFiles()
    {
        var files = FindGeneratedTableFiles();
        Debug.Log($"[TableSO] 발견된 생성 파일 {files.Count}개:");
        foreach (var file in files)
        {
            Debug.Log($"  - {file}");
        }
    }

    // TableCenter의 등록된 테이블 목록을 출력하는 디버그 메뉴
    [MenuItem("TableSO/Debug/Print Registered Tables")]
    public static void PrintRegisteredTables()
    {
        var tableCenter = FindTableCenter();
        if (tableCenter == null)
        {
            Debug.Log("[TableSO] TableCenter를 찾을 수 없습니다.");
            return;
        }

        Debug.Log("[TableSO] 등록된 테이블 목록:");
        
        var tableCenterType = tableCenter.GetType();
        string[] listFieldNames = { "csvTables", "assetTables", "refTables" };
        
        foreach (var fieldName in listFieldNames)
        {
            var field = tableCenterType.GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(tableCenter) as System.Collections.IList;
                if (list != null && list.Count > 0)
                {
                    Debug.Log($"  {fieldName}: {list.Count}개");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i] as ScriptableObject;
                        if (item != null)
                        {
                            Debug.Log($"    - {item.name} ({item.GetType().Name})");
                        }
                        else
                        {
                            Debug.Log($"    - [NULL 참조]");
                        }
                    }
                }
                else
                {
                    Debug.Log($"  {fieldName}: 0개");
                }
            }
            else
            {
                Debug.LogWarning($"  {fieldName}: 필드를 찾을 수 없음");
            }
        }
    }

    // 생성된 테이블들을 정리하는 메뉴 항목
    [MenuItem("TableSO/Clean Generated Table Assets")]
    public static void CleanGeneratedAssets()
    {
        if (EditorUtility.DisplayDialog("Confirm", 
            "생성된 모든 TableSO 에셋을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
            "삭제", "취소"))
        {
            CleanupGeneratedAssets();
        }
    }

    private static void CleanupGeneratedAssets()
    {
        try
        {
            if (Directory.Exists(GENERATED_TABLES_FOLDER))
            {
                var assetFiles = Directory.GetFiles(GENERATED_TABLES_FOLDER, "*.asset", SearchOption.AllDirectories);
                
                foreach (var file in assetFiles)
                {
                    string relativePath = file.Replace('\\', '/');
                    if (relativePath.StartsWith(Application.dataPath))
                    {
                        relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
                    }
                    AssetDatabase.DeleteAsset(relativePath);
                }

                Debug.Log($"[TableSO] {assetFiles.Length}개의 생성된 에셋을 정리했습니다.");
                AssetDatabase.Refresh();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] 에셋 정리 중 오류: {e.Message}");
        }
    }

    // 잘못 등록된 테이블들을 제거하는 메뉴 (null 참조 등)
    [MenuItem("TableSO/Debug/Clean Invalid Table References")]
    public static void CleanInvalidTableReferences()
    {
        var tableCenter = FindTableCenter();
        if (tableCenter == null)
        {
            Debug.Log("[TableSO] TableCenter를 찾을 수 없습니다.");
            return;
        }

        int removedCount = 0;
        var tableCenterType = tableCenter.GetType();
        string[] listFieldNames = { "csvTables", "assetTables", "refTables" };
        
        foreach (var fieldName in listFieldNames)
        {
            var field = tableCenterType.GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(tableCenter) as System.Collections.IList;
                if (list != null)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i] as ScriptableObject;
                        if (item == null)
                        {
                            list.RemoveAt(i);
                            removedCount++;
                        }
                    }
                }
            }
        }

        if (removedCount > 0)
        {
            EditorUtility.SetDirty(tableCenter);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TableSO] {removedCount}개의 잘못된 참조를 제거했습니다.");
        }
        else
        {
            Debug.Log("[TableSO] 제거할 잘못된 참조가 없습니다.");
        }
    }
}
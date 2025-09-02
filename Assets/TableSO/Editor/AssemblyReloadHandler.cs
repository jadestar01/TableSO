using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TableSO.Scripts;
using System.IO;
using TableSO.Scripts.Generator;
using Unity.VisualScripting;

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

        // 1단계: 모든 TableSO들을 먼저 생성하고 등록
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

        // 2단계: RefTableSO들의 참조 테이블 자동 할당 (모든 테이블이 등록된 후)
        EditorApplication.delayCall += () => {
            AssignRefTableReferences(tableCenter, tableSoTypes);
            
            if (createdCount > 0 || registeredCount > 0)
            {
                EditorUtility.SetDirty(tableCenter);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            CSVDataLoader.LoadAllCSVData();
        };
    }
    catch (Exception e)
    {
        Debug.LogError($"[TableSO] 초기화 중 오류: {e.Message}");
    }
}
private static void AssignRefTableReferences(TableCenter tableCenter, List<Type> tableSoTypes)
{
    try
    {
        Debug.Log("[TableSO] RefTable 참조 할당 단계 시작...");
        
        foreach (var tableType in tableSoTypes)
        {
            try
            {
                // IReferencable을 구현하는 RefTableSO인지 확인
                if (typeof(IReferencable).IsAssignableFrom(tableType))
                {
                    string assetName = tableType.Name;
                    string assetPath = Path.Combine(GENERATED_TABLES_FOLDER, $"{assetName}.asset");
                    var refTableAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                    if (refTableAsset != null && refTableAsset is IReferencable referencable)
                    {
                        AssignReferencesToRefTable(tableCenter, refTableAsset, referencable);
                        
                        // RefTableSO의 OnRefreshFromReferencedTables 호출하여 초기 데이터 설정
                        try
                        {
                            var refreshMethod = refTableAsset.GetType().GetMethod("RefreshFromReferencedTables");
                            if (refreshMethod != null)
                            {
                                refreshMethod.Invoke(refTableAsset, null);
                                Debug.Log($"[TableSO] RefTable {refTableAsset.name} 초기 데이터 새로고침 완료");
                            }
                        }
                        catch (Exception refreshEx)
                        {
                            Debug.LogWarning($"[TableSO] RefTable {refTableAsset.name} 새로고침 중 오류: {refreshEx.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TableSO] RefTable {tableType.Name} 참조 할당 중 오류: {e.Message}");
            }
        }
        
        Debug.Log("[TableSO] RefTable 참조 할당 완료");
    }
    catch (Exception e)
    {
        Debug.LogError($"[TableSO] RefTable 참조 할당 단계 중 전체 오류: {e.Message}");
    }
}

private static void AssignReferencesToRefTable(TableCenter tableCenter, ScriptableObject refTableAsset,
    IReferencable referencable)
{
    try
    {
        Debug.Log($"[TableSO] RefTable {refTableAsset.name}의 참조 테이블 할당 시작...");

        // IReferencable에서 필요한 참조 테이블 타입들 가져오기
        var requiredTableTypes = referencable.refTableTypes;
        
        Debug.Log($"@ 참조 테이블 개수 : {referencable.refTableTypes.Count}");
        foreach (var type in referencable.refTableTypes)
            Debug.Log($"@ {type}");

        int assignedCount = 0;
        foreach (var type in referencable.refTableTypes)
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
        Debug.Log($"[TableSO] RefTable {refTableAsset.name}에 {assignedCount}개의 참조 테이블이 할당되었습니다.");
    }
    catch (Exception e)
    {
        Debug.LogError($"[TableSO] RefTable {refTableAsset.name} 참조 할당 중 오류: {e.Message}");
    }
}

private static string GetTablePropertyName(string str)
{
    return str.Substring(0, str.Length - 2);
}

private static ScriptableObject FindTableInTableCenter(TableCenter tableCenter, Type targetType)
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
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        var scriptableObject = item as ScriptableObject;
                        if (scriptableObject != null && scriptableObject.GetType() == targetType)
                        {
                            return scriptableObject;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static bool AssignTableReference(ScriptableObject refTable, ScriptableObject targetTable, Type targetType)
    {
        try
        {
            var refTableType = refTable.GetType();

            // 필드 이름 생성 (예: CharacterTableSO -> characterTable)
            string fieldName = GetTableFieldName(targetType.Name);

            // private 필드 찾기
            var field = refTableType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null && field.FieldType == targetType)
            {
                field.SetValue(refTable, targetTable);
                Debug.Log($"[TableSO] 필드 {fieldName}에 {targetTable.name} 할당 완료");
                return true;
            }
            else
            {
                Debug.LogWarning($"[TableSO] RefTable {refTable.name}에서 {fieldName} 필드를 찾을 수 없거나 타입이 맞지 않습니다.");

                // 디버깅을 위해 모든 필드 출력
                var allFields = refTableType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                Debug.Log($"[TableSO] 사용 가능한 필드들: {string.Join(", ", allFields.Select(f => $"{f.Name}({f.FieldType.Name})"))}");
            
                // 대안적 방법: 타입 이름 기반으로 다시 시도
                var alternativeField = allFields.FirstOrDefault(f => 
                    f.FieldType == targetType && 
                    f.Name.ToLower().Contains(GetTablePropertyName(targetType.Name).ToLower()));
                
                if (alternativeField != null)
                {
                    alternativeField.SetValue(refTable, targetTable);
                    Debug.Log($"[TableSO] 대안 필드 {alternativeField.Name}에 {targetTable.name} 할당 완료");
                    return true;
                }
            }
        
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] 테이블 참조 할당 중 오류: {e.Message}");
            return false;
        }
    }


    private static string GetTableFieldName(string typeName)
    {
        // "TableSO" 접미사 제거
        string baseName = typeName;
        if (baseName.EndsWith("TableSO"))
        {
            baseName = baseName.Substring(0, baseName.Length - 7);
        }

        // 첫 글자를 소문자로 만들고 "Table" 접미사 추가
        if (!string.IsNullOrEmpty(baseName))
        {
            baseName = char.ToLower(baseName[0]) + baseName.Substring(1) + "Table";
        }

        return baseName;
    }


    private static void UpdateRefTableReferences(ScriptableObject refTable)
    {
        try
        {
            // RefTableSO의 referencedTables 리스트 업데이트
            var refTableType = refTable.GetType();
            var referencedTablesField = refTableType.BaseType?.GetField("referencedTables",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (referencedTablesField != null)
            {
                var referencedTablesList = referencedTablesField.GetValue(refTable) as List<ScriptableObject>;
                if (referencedTablesList != null)
                {
                    referencedTablesList.Clear();

                    // 할당된 테이블들을 referencedTables 리스트에 추가
                    var fields = refTableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType.IsSubclassOf(typeof(ScriptableObject)) &&
                            field.Name.EndsWith("Table"))
                        {
                            var tableValue = field.GetValue(refTable) as ScriptableObject;
                            if (tableValue != null && !referencedTablesList.Contains(tableValue))
                            {
                                referencedTablesList.Add(tableValue);
                            }
                        }
                    }

                    Debug.Log($"[TableSO] {refTable.name}의 referencedTables 리스트에 {referencedTablesList.Count}개 테이블 추가");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TableSO] RefTable 참조 리스트 업데이트 중 오류: {e.Message}");
        }
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
}
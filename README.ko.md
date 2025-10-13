# TableSO: 당신의 올인원 데이터 관리 도구 🛠️

[English](README.md) | [한국어](README.ko.md)

[![Unity](https://img.shields.io/badge/Made%20with-Unity-black?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](https://opensource.org/licenses/MIT)


**TableSO**는 Unity에서 **ScriptableObject**와 **Addressables**를 기반으로 구조화된 데이터와 테이블을 자동으로 생성하고 관리하는 강력한 툴입니다. `.csv` 파일, 폴더에 있는 에셋, 그리고 다른 테이블을 참조하는 커스텀 테이블을 손쉽게 처리하여 게임 데이터를 효율적으로 관리할 수 있도록 돕습니다.

## 튜토리얼
https://www.youtube.com/watch?v=siwB-cHZhvQ

---

## 🧐 의존성

TableSO는 에셋과 CSV를 원활하게 처리하기 위해 다음 Unity 패키지를 사용합니다.

* **Addressables**: CSV와 에셋을 해결하고 관리하는 데 필수적입니다.
    * *Addressables에 대해 잘 몰라도 걱정하지 마세요. 모든 과정은 자동으로 처리되어 사용자의 관심 범위 밖에서 동작합니다.*
* **Unity Test Framework**: TableSO의 핵심 기능이 다양한 환경에서 올바르게 동작하는지 검증하기 위해 사용됩니다.
    * *최종 사용자가 이 테스트를 직접 실행할 필요는 없으며, 패키지 개발 단계에서만 활용됩니다.*

---

## ✨ 주요 장점

* **확장 가능한 ScriptableObject**: SO에 데이터뿐만 아니라 `GetRandomItem()`이나 `GetItemListByType(type)`과 같은 유틸리티 메서드를 추가할 수 있습니다.
* **데이터와 테이블의 분리**: 데이터에 변경이 발생해도, `UpdateData` 버튼을 통해 기존 테이블 구조에 영향을 주지 않고 CSV를 업데이트할 수 있으며, 에셋은 Addressables Group을 사용해 구조에 영향이 없습니다.
* **자동 에셋 등록**: 폴더에 에셋을 추가하기만 하면 자동으로 Addressables에 등록됩니다.
* **원활한 업데이트**: **`Update Csv`** 기능을 사용하여 테이블 구조나 메서드를 변경하지 않고도 `.csv` 파일의 데이터만 쉽게 수정할 수 있습니다.
* **영구적인 데이터**: 데이터는 한 번 로드되면 씬 변경 시에도 유지되므로 반복 로드가 필요 없습니다.
* **고성능**: 효율적인 **Dictionary 캐싱**을 통해 빠른 데이터 검색을 제공합니다.

---

## 다운로드 및 환경 설정

1.  Unity 상단 탭의 `Window` > `Package Manager`를 엽니다.
2.  좌상단의 `+` 버튼을 클릭하고 `Add package from git URL...`을 선택합니다.
3.  아래 URL을 입력하고 설치합니다.
    ```
    https://github.com/jadestar01/TableSO.git?path=Packages/com.jadestar01.tableso#v1.0.4
    ```
4.  Unity 상단 메뉴 탭의 `TableSO`를 눌러 **TableEditor**를 실행합니다.
5.  Editor의 **Center** 탭에서 `Create TableCenter`를 클릭하여 디렉토리 구조와 Addressable 설정을 완료하고, **TableCenter**를 생성합니다.

## 🗺️ 작동 방식

### 1. TableEditor

**TableEditor**는 모든 테이블을 관리하는 중앙 허브입니다. 다음 방법으로 열 수 있습니다.
* **키보드 단축키**: 윈도우에서는 `[CTRL + T]`, 맥에서는 `[CMD + T]`.
* **메뉴 탭**: 유니티 에디터 상단의 `[TableSO]` 탭을 통해 열 수 있습니다.


### 2. TableCenter

**TableCenter**는 모든 테이블의 참조를 관리하는 중앙 저장소 역할을 합니다. `GetTable<T>` 메서드를 사용하여 특정 테이블을 쉽게 가져올 수 있습니다.
* **Initialize()**: 모든 테이블의 데이터를 갱신하기 위해 Initialize()를 필요로 합니다. 이를 통해 데이터를 읽어올 수 있습니다.
* **OnInitialized**: 이벤트를 통해 데이터 로딩이 끝난 이후, 씬 전환/특정 행동 수행 등 메서드를 등록할 수 있습니다.
```csharp
using TableSO.Scripts;
using Table;

public class TableLoader : MonoBehaviour
{
    public TableCenter center;

    private void Awake()
    {
        center.OnInitialized += AfterInitialize;
        center.Initialize();
    }

    private void AfterInitialize()
    {
        Debug.Log("Init Complete");
        var table = center.GetTable<DamageExpressionDataTableSO>();
    }
}
```


---

## 📂 테이블 종류

TableSO는 모든 데이터 요구사항을 충족시키기 위해 세 가지 고유한 테이블 유형을 지원합니다.

### 1. CsvTable

* **구조**: `.csv` 파일을 읽어 `[TKey, TData]` 형태의 테이블을 생성합니다. 파일 내용에 따라 `TKey`를 해결하고 `TData`를 생성합니다.
* **CSV 규칙**:
    * **첫 번째 행**: **변수 이름**을 포함해야 합니다. (표준 변수 명명 규칙을 따르세요.)
    * **두 번째 행**: **변수 타입**을 포함합니다. (`enum`과 `array` 타입을 지원합니다.)
    * **첫 번째 열**: 반드시 **ID**라는 변수이름 이어야 합니다. (반드시 고유해야 합니다. 즉, 중복 없어야 합니다.)
    * **배열**: `|`를 사용하여 요소를 구분합니다 (예: `1|2|3`).
* 각 행을 기반으로 `DataClass`가 생성되며, `ID`를 기반으로 `TableClass`가 생성됩니다.


### 2. AssetTable

* **구조**: 지정된 폴더에서 데이터를 읽는 `[string, TData]` 형태의 테이블입니다. 파일 이름(`string`)을 `TKey`로 사용하고 `TData`를 생성합니다.
* 읽을 에셋을 필터링하여 지정할 수 있습니다.
* 모든 에셋은 **Addressables**로 저장되며, 게임 시작 시 자동으로 로드됩니다.
* **지원 에셋 필터 종류**: Sprite, Prefab, ScriptableObject, Texture2D, AudioClip, AnimationClip, Material, TextAsset


### 3. CustomTable

* **구조**: 다른 테이블(`CsvTable`, `AssetTable` 등)을 참조하여 복합적인 데이터를 생성하는 `[TKey, TData]` 형태의 테이블입니다.
* 생성하기 전에 연결할 관련 테이블을 선택할 수 있습니다.
* **사용자가 `TData`를 직접 정의**해야 하며(생성자와 필드 정의), 연결된 테이블에서 데이터를 읽어와 데이터 리스트를 구축하는 `UpdateData()` 메서드의 로직을 직접 구현해야 합니다.

---

## 👩‍💻 사용 예시: 아이템 관리

스프라이트와 아이템 정보를 통합하여 아이템 시스템을 만든다고 가정해 봅시다. TableSO를 사용하는 방법은 다음과 같습니다.

0.  **`TableCenter`에 대한 Initialize: Loading, Title 등 메뉴에서 TableCenter를 Initialize 합니다.
1.  **`ItemSprite`에 대한 `AssetTable` 생성**: **Sprite 필터**를 사용하여 모든 아이템 스프라이트를 포함합니다.
2.  **`ItemData`에 대한 `CsvTable` 생성**: 이 테이블은 스프라이트 이름을 변수로 포함하여 모든 아이템 정보를 담습니다.
3.  **`Item`에 대한 `CustomTable` 생성**: 이것이 최종적으로 통합된 아이템 클래스가 될 것입니다.
4.  **모든 테이블 새로고침**: TableEditor의 **Center** 탭으로 가서 **`RefreshAllTable`**을 클릭하여 ScriptableObject를 생성하고 테이블 참조를 해결합니다.
5.  **클래스 정의**: 자동으로 생성된 `Item` 클래스에 `ItemData`와 `ItemSprite` 필드를 정의하고 생성자를 구축합니다.
6.  **업데이트 로직 구현**: `ItemCustomTable`에서 `UpdateData` 로직을 직접 구현하여 `ItemSpriteAssetTable`과 `ItemDataCsvTable`의 데이터를 사용하여 최종 `Item` 객체를 구축합니다.
7.  **스크립트에서 접근**: `tableCenter`를 참조하는 스크립트를 작성합니다.
8.  **테이블 가져오기**: `tableCenter.GetTable<ItemCustomTable>()`을 사용하여 병합된 아이템 테이블을 가져옵니다.
9.  **데이터 검색**: `GetAllKey()`를 사용하여 모든 아이템 키 목록을 가져오고, `GetData(key)`로 특정 `Item` 객체에 접근합니다.

---

## 시나리오: Data와 Asset을 통해 UI에 표기하는 예제

이 예제는 CSV 데이터와 에셋을 결합하여 UI에 표시하는 과정을 보여줍니다.

### 1. Data 테이블 만들기 (CsvTable)

1.  자동 생성된 경로 (`Assets/TableSO/Data`)에 `ExampleData.csv` 파일을 생성합니다.
    * 첫 번째 행은 **요소의 이름**, 두 번째 행은 **요소의 타입**을 정의합니다.
    * 배열형은 `|` 기호로 원소를 구분하고, 열거형은 자동으로 대상을 추적합니다.
2.  TableSOEditor를 열고, **Csv** 탭에서 `ExampleData.csv`를 선택하고 코드를 생성합니다.
3.  **Center** 탭으로 이동하여 **`Refresh All Tables`**를 눌러 TableCenter에 테이블을 등록합니다.
    * 이제 `ExampleData.csv`는 유니티에서 참조 가능한 형태로 변경됩니다.

| ID | IconName | Text |
|----|----------|------|
| int | string[] | string |
| 1 | T | Hello |
| 2 | T\|A | World |
| 3 | T\|A\|B | TableSO |
| 4 | T\|A\|B\|L | Is |
| 5 | T\|A\|B\|L\|E | Fun |
| 6 | T\|A\|B\|L\|E\|S | And |
| 7 | T\|A\|B\|L\|E\|S\|O | Easy |

*이 테이블은 IconName 필드를 통해 Icon Sprite Asset의 파일 이름을 배열로 참조합니다.*


### 2. Asset 테이블 만들기 (AssetTable)

1.  자동 생성된 경로 (`Assets/TableSO/Asset`)에 원하는 에셋이 담긴 폴더 (`ExampleIcon`)를 생성하고 스프라이트 에셋을 넣습니다.
2.  TableSOEditor를 열고, **Asset** 탭에서 `ExampleIcon` 폴더를 선택하고, **Type**을 **Sprite**로 선택한 후 코드를 생성합니다.
3.  **Center** 탭으로 이동하여 **`Refresh All Tables`**를 눌러 TableCenter에 테이블을 등록합니다.
    * 이제 `ExampleIcon` 폴더 내의 모든 스프라이트 에셋은 유니티에서 참조 가능한 형태로 변경됩니다.
    

### 3. Custom 테이블 만들기 (CustomTable)

데이터(`ExampleData.csv`)와 에셋(`ExampleIcon` 폴더)의 연결을 정의하여 최종적으로 사용할 통합 테이블을 만듭니다.

1.  TableSOEditor를 열고, **Custom** 탭으로 이동합니다.
2.  참조할 테이블로 `ExampleDataTableSO`와 `ExampleIconAssetTableSO`를 선택합니다.
3.  최종 클래스 이름으로 **Example**을 입력하고, 키 타입은 `ExampleData`와 동일한 **int** 형을 선택합니다.
4.  **Refresh All Tables**를 다시 실행하여 테이블 참조를 해결합니다.
5.  생성된 데이터 클래스인 **Example**과 테이블 클래스인 **ExampleTableSO**를 아래와 같이 직접 작성하여 두 테이블의 연결을 정의합니다.

#### Example.cs (데이터 클래스 정의)
```csharp
namespace TableData
{
    [System.Serializable]
    public class Example : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        public List<Sprite> Icons = new List<Sprite>();

        public string Text;

        // 생성자를 통해 데이터를 구성합니다.
        public Example(int ID, List<Sprite> Icons, string Text)
        {
            this.ID = ID;
            this.Icons = Icons;
            this.Text = Text;
        }
    }
}
```

#### ExampleTableSO.cs (데이터 업데이트 로직 구현)
```csharp
namespace Table
{
    using TableData;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

    public class ExampleTableSO : TableSO.Scripts.CustomTableSO<int, Example>
    {
        public override TableType tableType => TableType.Custom;
        public string fileName => "ExampleTableSO";

        [SerializeField] private ExampleDataTableSO ExampleDataTable;
        [SerializeField] private ExampleIconAssetTableSO ExampleIconAssetTable;

        // 참조할 테이블 타입을 명시합니다.
        public override List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(ExampleDataTableSO),
            typeof(ExampleIconAssetTableSO),
        };

        // CustomTable의 핵심: 다른 테이블의 데이터를 읽어와 최종 데이터를 구축합니다.
        public override async Task UpdateData()
        {
            ReleaseData(); // 기존 데이터 비우기

            foreach (var id in ExampleDataTable.GetAllKey())
            {
                List<Sprite> icons = new List<Sprite>();
                
                // CsvTable의 IconName 배열(파일 이름)을 사용해 AssetTable에서 Sprite 에셋을 가져옵니다.
                foreach (var iconName in ExampleDataTable.GetData(id).IconName)
                    icons.Add(ExampleIconAssetTable.GetData(iconName).Asset);
                
                // 최종 Example 객체를 생성하고 dataList에 추가합니다.
                dataList.Add(new Example(id, icons, ExampleDataTable.GetData(id).Text));
            }

            base.UpdateData(); // 캐싱 업데이트
        }
        
        // GetData 로직은 base.GetData(key)를 사용할 수 있지만, 필요에 따라 오버라이드 가능합니다.
        public override Example GetData(int key)
        {
            return base.GetData(key);
        }
    }
}
```

### 4. 코드에서 접근
최종적으로 코드에서는 아래와 같이 접근하여 UI에 데이터를 표시할 수 있습니다.
```csharp
    // tableCenter는 미리 참조되어 있어야 합니다.
    var table = tableCenter.GetTable<ExampleTableSO>(); 
    var data = table.GetData(id); // ID를 사용하여 통합된 Example 객체를 가져옵니다.

    // 통합된 데이터 사용 예시 (Icons 리스트와 Text)
    foreach (var icon in data.Icons)
    {
        Image image = GetImage(); // UI Image 컴포넌트를 가져오는 사용자 함수
        image.sprite = icon;
    }

    text.text = data.Text;
```

# TableSO: 당신의 올인원 데이터 관리 도구 🛠️

TableSO는 유니티에서 **ScriptableObject**와 **Addressables**를 기반으로 구조화된 데이터와 테이블을 자동으로 생성하고 관리하는 강력한 툴입니다. `.csv` 파일, 폴더, 그리고 다른 테이블을 참조하는 커스텀 테이블을 손쉽게 처리하여 게임 데이터를 효율적으로 관리할 수 있도록 도와줍니다.

## 🧐 의존성

TableSO는 에셋과 CSV를 원활하게 처리하기 위해 다음을 사용합니다.

* **Addressables**: CSV와 에셋을 해결하고 관리하는 데 필수적입니다.
  * Addressables를 잘 몰라도 걱정하지마세요. 모든 과정은 자동화되어 사용자의 관심범위 밖에서 동작합니다.

---

## ✨ 주요 장점

* **확장 가능한 ScriptableObject**: SO에 데이터뿐만 아니라 `GetRandomItem()`이나 `GetItemListByType(type)`과 같은 유틸리티 메서드를 추가할 수 있습니다.
* **Table과 Data 분리** : Data에 변경이 발생해도, Csv는 UpdateData 버튼으로 기존 구조에 영향이 없으며, Asset은 Addressable Group을 쓰기에 구조에 영향이 없습니다.
* **자동 에셋 등록**: 폴더에 에셋을 추가하기만 하면 자동으로 등록됩니다.
* **원활한 업데이트**: `Update Csv` 기능을 사용하여 테이블 구조나 메서드를 변경하지 않고 `.csv` 파일의 데이터를 수정할 수 있습니다.
* **영구적인 데이터**: 데이터는 한 번 로드되면 씬 변경 시에도 유지되므로 반복 로드가 필요 없습니다.
* **고성능**: 효율적인 **Dictionary 캐싱**을 통해 빠른 검색을 제공합니다.

---

## 다운로드 및 환경 설정

1. Unity 상단 탭의 Windows > PackageManager > 좌상단 + 버튼 > install package from git URL
 - 입력 : https://github.com/jadestar01/TableSO.git?path=Packages/com.jadestar01.tableso#v1.0.0
2. Unity 상단 탭의 TableSO를 눌러 Editor를 실행합니다.
3. Editor의 Center 탭의 'Create TableCenter'를 클릭하여 디렉토리의 구조와 Addressable 설정을 마치고, Table Center가 생성됩니다.

## 🗺️ 작동 방식

### 1. TableEditor

**TableEditor**는 모든 테이블을 관리하는 중앙 허브입니다. 다음 방법으로 열 수 있습니다.
* **키보드 단축키**: 윈도우에서는 `[CTRL + T]`, 맥에서는 `[CMD + T]`.
* **메뉴 탭**: 유니티 에디터 상단의 `[TableSO]` 탭을 통해 열 수 있습니다.

### 2. TableCenter

**TableCenter**는 모든 테이블의 참조를 관리하는 중앙 저장소 역할을 합니다. `GetTable<T>` 메서드를 사용하여 특정 테이블을 쉽게 가져올 수 있습니다.

---

## 📂 테이블 종류

TableSO는 모든 데이터 요구사항을 충족시키기 위해 세 가지 고유한 테이블 유형을 지원합니다.

### 1. CsvTable

* **구조**: `.csv` 파일을 읽는 `[TKey, TData]` 형태의 테이블입니다. 파일 내용에 따라 `TKey`를 해결하고 `TData`를 생성합니다.
* **CSV 규칙**:
    * **첫 번째 행**: **변수 이름**을 포함해야 합니다. 표준 변수 명명 규칙을 따르세요.
    * **두 번째 행**: **변수 타입**을 포함합니다. `enum`과 `array` 타입을 지원합니다.
    * **배열**: `|`를 사용하여 요소를 구분합니다 (예: `1|2|3`).
* 각 행을 기반으로 `DataClass`가 생성되며, `ID`를 기반으로 `TableClass`가 생성됩니다.

### 2. AssetTable

* **구조**: 폴더에서 데이터를 읽는 `[string, TData]` 형태의 테이블입니다. 파일 이름(`string`)을 `TKey`로 사용하고 `TData`를 생성합니다.
* 읽을 에셋을 필터링하여 지정할 수 있습니다.
* 모든 에셋은 **Addressables**로 저장되며, 게임 시작 시 자동으로 로드됩니다.

### 3. MergeTable

* **구조**: 다른 테이블(`CsvTable`, `AssetTable` 등)을 참조하여 복합적인 데이터를 생성하는 `[TKey, TData]` 형태의 테이블입니다.
* 생성하기 전에 연결할 관련 테이블을 선택할 수 있습니다.
* **사용자가 `TData`를 직접 정의**해야 하며(생성자와 필드 정의), 연결된 테이블에서 데이터를 읽어와 데이터 리스트를 구축하는 `UpdateData()` 메서드의 로직을 직접 구현해야 합니다.

---

## 👩‍💻 사용 예시: 아이템 관리

스프라이트와 아이템 정보를 통해 아이템 시스템을 만든다고 가정해 봅시다. TableSO를 사용하는 방법은 다음과 같습니다.

1.  **`ItemSprite`에 대한 `AssetTable` 생성**: **Sprite 필터**를 사용하여 모든 아이템 스프라이트를 포함합니다.
2.  **`ItemData`에 대한 `CsvTable` 생성**: 이 테이블은 스프라이트 이름을 변수로 포함하여 모든 아이템 정보를 담습니다.
3.  **`Item`에 대한 `MergeTable` 생성**: 이것이 최종적으로 통합된 아이템 클래스가 될 것입니다.
4.  **모든 테이블 새로고침**: TableEditor의 **Center** 탭으로 가서 `RefreshAllTable`을 클릭하여 ScriptableObject를 생성하고 참조를 해결합니다.
5.  **클래스 정의**: 자동으로 생성된 `Item` 클래스에 `ItemData`와 `ItemSprite` 필드를 정의하고 생성자를 구축합니다.
6.  **업데이트 로직 구현**: `ItemMergeTable`에서 `UpdateData` 로직을 구현하여 `ItemSpriteAssetTable`과 `ItemDataCsvTable`의 데이터를 사용하여 `Item` 객체를 구축합니다.
7.  **스크립트에서 접근**: `ItemManager` 스크립트(또는 다른 스크립트)에서 `tableCenter`를 참조합니다.
8.  **테이블 가져오기**: `tableCenter.GetTable<ItemMergeTable>()`을 사용하여 병합된 아이템 테이블을 가져옵니다.
9.  **데이터 검색**: `GetAllKey()`를 사용하여 모든 아이템 키 목록을 가져오고, `GetData(key)`로 특정 `Item` 객체에 접근합니다.

## 시나리오 : Data와 Asset을 통해 UI에 표기하는 예제

### 1. Data 테이블 만들기
 - 자동 생성된 경로 `Assets/TableSO/Data`에 원하는 데이터가 담긴 파일인 ExampleData.csv 를 생성합니다.
    - 첫번째 행은 반드시 요소의 이름입니다.
    - 두번째 행은 반드시 요소의 타입입니다.
    - 배열형은 '\|' 기호로 원소를 구분합니다.
    - 열거형은 자동으로 대상을 추적합니다.
 - TableSOEditor를 열고, Csv 탭으로 이동합니다. 이후 Browse를 눌러 ExampleData.csv를 선택하고, 버튼을 눌러 코드를 생성합니다.
 - 코드 생성이 완료되면, Center 탭으로 이동합니다. 이후 Refresh All Tables를 눌러 Center에 Table을 등록합니다.
 - 이제 ExampleData.csv는 유니티에서 참조 가능한 형태로 변경되었습니다.

| ID | IconName | Text |
|----|----------|------|
| int | string[] | string |
| 1 | T | Hello |
| 2 | T\|A | World |
| 3 | T\|A\|B | TableSO |
| 1 | T\|A\|B\|L | Is |
| 2 | T\|A\|B\|L\|E | Fun |
| 3 | T\|A\|B\|L\|E\|S | And |
| 3 | T\|A\|B\|L\|E\|S\|O | Easy |

 - 위 테이블은 IconName이라는 Icon Sprite Asset의 파일 이름을 참조합니다.


### 2. Asset 테이블 만들기
 - 자동 생성된 경로 `Assets/TableSO/Asset`에 원하는 에셋이 담긴 폴더인 ExampleIcon 을 생성합니다.
    - AssetTable은 폴더 내의 특정 Type만을 추출합니다.
    - 이는 기본적인 폴더 구조에 대한 반영입니다.
    - 복잡한 Asset Table 구축을 위해선 Custom Table을 사용하십시오.
 - TableSOEditor를 열고, Asset 탭으로 이동합니다. 이후 Browse를 눌러 ExampleIcon 폴더를 선택하고, Type을 선택하고, 버튼을 눌러 코드를 생성합니다.
 - 코드 생성이 완료되면, Center 탭으로 이동합니다. 이후 Refresh All Tables를 눌러 Center에 Table을 등록합니다.
 - 이제 ExampleIcon의 Type이 같은 모든 에셋들은 유니티에서 참조 가능한 형태로 변경되었습니다.


### 3. Custom 테이블 만들기
 - ExampleData.csv는 ExampleIcon의 Icon을 배열로 참조합니다.
 - 이는 물론 코드에서 해결할 수 있는 참조이기도 하지만, 자주 사용된다면, Custom 테이블로 묶는 것이 좋습니다.
 - TableSOEditor를 열고, Custom 탭으로 이동합니다. 이후 ExampleDataCsvTableSO와 ExampleIconAssetTableSO를 선택합니다.
 - Table Name에 최종적인 클래스 명을 작성합시다. 여기선 Example이라는 이름을 사용하겠습니다.
 - 사용할 키 타입을 선택해줍시다. 여기선 ExampleData가 ExampleIcon을 다수 참조하니, ExampleData와 같은 int 형 키를 사용합니다.
 - TableSOEditor를 열고, Asset 탭으로 이동합니다. 이후 Browse를 눌러 ExampleIcon 폴더를 선택하고, Type을 선택하고, 버튼을 눌러 코드를 생성합니다.
 - 코드 생성이 완료되면, Center 탭으로 이동합니다. 이후 Refresh All Tables를 눌러 Center에 Table을 등록합니다.
 - 아직 유니티에서 제대로 참조가 불가한 형태입니다. 두 테이블의 연결을 정의해줘야 하며, 이를 위해서 Custom Table은 사전에 선택한 테이블들에 대한 참조를 해결해줍니다.
 - 이후, 생성된 데이터 클래스인 `Example`과 테이블 클래스인 `ExampleTableSO`를 작성해주면 됩니다.
 - `Example`의 경우 아래와 같이 작성해서 생성자로 Data를 구성하게 만듭시다.

#### Example.cs
```csharp
namespace TableData
{
    [System.Serializable]
    public class Example : IIdentifiable<int>
    {
        [field: SerializeField] public int ID { get; internal set; }

        public List<Sprite> Icons = new List<Sprite>();

        public string Text;

        public Example(int ID, List<Sprite> Icons, string Text)
        {
            this.ID = ID;
            this.Icons = Icons;
            this.Text = Text;
        }
    }
}
```

 - `ExampleTableSO`의 경우 아래와 같이 작성해서, 모든 참조를 해결하고 dataList에 추가하도록 합시다.
    - 이 과정 전에는 ReleaseData를 해줌으로써, 데이터를 비워주고, 후에는 UpdateData를 해줌으로써, 캐싱할 수 있습니다.

#### ExampleTableSO.cs
```csharp
namespace Table
{
    public class ExampleTableSO : TableSO.Scripts.CustomTableSO<int, TableData.Example>
    {
        public override TableType tableType => TableType.Custom;

        public string fileName => "ExampleTableSO";
        [SerializeField] private ExampleDataTableSO ExampleDataTable;
        [SerializeField] private ExampleIconAssetTableSO ExampleIconAssetTable;

        public override List<Type> refTableTypes { get; set; } = new List<Type>()
        {
            typeof(ExampleDataTableSO),
            typeof(ExampleIconAssetTableSO),
        };

        public override async Task UpdateData()
        {
            ReleaseData();

            foreach (var id in ExampleDataTable.GetAllKey())
            {
                List<Sprite> icons = new List<Sprite>();
                foreach (var icon in ExampleDataTable.GetData(id).IconName)
                    icons.Add(ExampleIconAssetTable.GetData(icon).Asset);
                
                dataList.Add(new TableData.Example(id, icons, ExampleDataTable.GetData(id).Text));
            }

            base.UpdateData();
        }

        public override TableData.Example GetData(int key)
        {
            // TODO: Implement GetData logic
            return base.GetData(key);
        }
    }
}
```
 - 결과적으로 코드에서는 아래와 같이 접근할 수 있습니다.
```csharp
    var table = tableCenter.GetTable<ExampleMergeTableSO>();
    var data = table.GetData(id);

    foreach (var icon in data.Icons)
    {
        Image image = GetImage();
        image.sprite = icon;
    }

    text.text = data.Text;
```

---


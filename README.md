# TableSO: Your All-in-One Data Management Tool 🛠️

[English](README.md) | [한국어](README.ko.md)

[![Unity](https://img.shields.io/badge/Made%20with-Unity-black?style=for-the-badge&logo=unity&logoColor=white)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](https://opensource.org/licenses/MIT)

**TableSO** is a powerful tool for Unity that automatically generates and manages structured data and tables based on **ScriptableObject** and **Addressables**. It efficiently handles `.csv` files, assets within folders, and custom tables that reference other data, helping you streamline your game data management.

## 🧐 Dependencies

TableSO relies on the following Unity packages to seamlessly handle assets and CSVs:

* **Addressables**: Essential for resolving and managing CSV data and assets.
    * *Don't worry if you're unfamiliar with Addressables. The entire process is automated and operates outside the user's focus.*
* **Unity Test Framework**: Used to verify that TableSO's core features function correctly across various environments.
    * *End-users do not need to run these tests; they are utilized only during the package development phase.*

---

## ✨ Key Advantages

* **Extensible ScriptableObject**: Add utility methods like `GetRandomItem()` or `GetItemListByType(type)` directly to your SO, not just raw data.
* **Decoupled Table and Data**: Changes to data in the CSV can be updated using the **`UpdateData`** button without affecting the existing table structure. Assets use Addressable Groups, maintaining structural integrity.
* **Automatic Asset Registration**: Simply add assets to a designated folder, and they are automatically registered as Addressable assets.
* **Seamless Updates**: Use the **`Update Csv`** feature to modify data in `.csv` files without altering the table structure or methods.
* **Persistent Data**: Once data is loaded, it remains available even when scenes change, eliminating the need for repeated loading.
* **High Performance**: Provides fast lookup times through efficient **Dictionary caching**.

---

## Download and Setup

1.  In the Unity Editor's top bar, navigate to `Window` > `Package Manager`.
2.  Click the `+` button in the upper-left corner and select `Add package from git URL...`.
3.  Enter the following URL and install the package:
    ```
    https://github.com/jadestar01/TableSO.git?path=Packages/com.jadestar01.tableso#v1.0.2
    ```
4.  Run the **TableEditor** by clicking the **`TableSO`** tab in the Unity top menu.
5.  In the Editor's **Center** tab, click **`Create TableCenter`** to finalize the directory structure and Addressable settings, creating the central **TableCenter** asset.

## 🗺️ How it Works

### 1. TableEditor

The **TableEditor** is the central hub for managing all your tables. It can be opened via:
* **Keyboard Shortcut**: `[CTRL + T]` on Windows, `[CMD + T]` on Mac.
* **Menu Tab**: Through the `[TableSO]` tab in the Unity Editor's main menu.

### 2. TableCenter

The **TableCenter** acts as the central repository, managing references to all tables. You can easily retrieve a specific table using the generic method `GetTable<T>`.

---

## 📂 Table Types

TableSO supports three distinct table types to satisfy all your data requirements.

### 1. CsvTable

* **Structure**: A `[TKey, TData]` table that reads from a `.csv` file. It resolves `TKey` and generates `TData` based on the file content.
* **CSV Rules**:
    * **First Row**: Must contain **variable names**. (Follow standard variable naming conventions.)
    * **Second Row**: Must contain **variable types**. (`enum` and `array` types are supported.)
    * **First column**: The variable name must be **ID**.
    * **Arrays**: Elements are separated using the pipe character `|` (e.g., `1|2|3`).
* A `DataClass` is generated based on each row, and a `TableClass` is generated based on the `ID`.

### 2. AssetTable

* **Structure**: A `[string, TData]` table that reads data from a designated folder. It uses the file name (`string`) as the `TKey` and generates `TData`.
* You can filter and specify the asset types to be read.
* All assets are stored as **Addressables** and are loaded automatically at game start.
* **Supported Asset Filters**: Sprite, Prefab, ScriptableObject, Texture2D, AudioClip, AnimationClip, Material, TextAsset

### 3. CustomTable

* **Structure**: A `[TKey, TData]` table that generates complex data by referencing other tables (`CsvTable`, `AssetTable`, etc.).
* You must select the related tables to link before creation.
* **The user must define `TData` directly** (constructor and field definitions) and implement the logic within the `UpdateData()` method to read data from the linked tables and build the final data list.

---

## 👩‍💻 Usage Example: Item Management

Let's assume you want to create an item system by combining item sprites and item information. Here's how to use TableSO:

1.  **Create an `AssetTable` for `ItemSprite`**: Use the **Sprite filter** to include all item sprites from a folder.
2.  **Create a `CsvTable` for `ItemData`**: This table holds all item information, including the sprite name as a variable.
3.  **Create a `CustomTable` for `Item`**: This will be the final, integrated item class.
4.  **Refresh All Tables**: Go to the **Center** tab of the TableEditor and click **`RefreshAllTable`** to generate ScriptableObjects and resolve references.
5.  **Define Classes**: Define `ItemData` and `ItemSprite` fields in the automatically generated `Item` class and set up the constructor.
6.  **Implement Update Logic**: Implement the **`UpdateData`** logic in `ItemCustomTable` to use data from `ItemSpriteAssetTable` and `ItemDataCsvTable` to construct the final `Item` objects.
7.  **Access in Script**: Reference the `tableCenter` in your `ItemManager` (or other script).
8.  **Get the Table**: Use `tableCenter.GetTable<ItemCustomTable>()` to retrieve the merged item table.
9.  **Search Data**: Access all item keys using `GetAllKey()`, and retrieve a specific `Item` object using `GetData(key)`.

---

## Scenario: Displaying Data and Assets in UI

This scenario demonstrates combining CSV data and assets using a Custom Table for UI display.

### 1. Create a Data Table (CsvTable)

1.  Create a file named `ExampleData.csv` in the automatically generated path: `Assets/TableSO/Data`.
    * The first row must be **element names**, and the second row must be **element types**.
    * Array types are separated by the `|` character. Enums are tracked automatically.
2.  Open the TableSOEditor, navigate to the **Csv** tab, select `ExampleData.csv` via `Browse`, and click the button to generate the code.
3.  Go to the **Center** tab and click **`Refresh All Tables`** to register the Table with the Center.
    * `ExampleData.csv` is now available as a referenceable object in Unity.

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

*This table references Icon Sprite Asset file names in the `IconName` array.*

### 2. Create an Asset Table (AssetTable)

1.  Create a folder named `ExampleIcon` and place your desired assets (e.g., Sprite assets) in the automatically generated path: `Assets/TableSO/Asset`.
2.  Open the TableSOEditor, navigate to the **Asset** tab, select the `ExampleIcon` folder via `Browse`, choose the **Type** (e.g., **Sprite**), and generate the code.
3.  Go to the **Center** tab and click **`Refresh All Tables`** to register the Table with the Center.
    * All assets of the selected type within the `ExampleIcon` folder are now referenceable in Unity.

### 3. Create a Custom Table (CustomTable)

The CSV data references assets, so we combine them using a Custom Table for easy access.

1.  Open the TableSOEditor and navigate to the **Custom** tab.
2.  Select `ExampleDataTableSO` and `ExampleIconAssetTableSO` as the tables to link.
3.  Enter the final class name (e.g., **`Example`**) in the Table Name field.
4.  Select the Key Type, which should be the same as the data key type (e.g., **`int`**), as `ExampleData` is keyed by `int`.
5.  Click **`Refresh All Tables`** again to resolve references.
6.  You must now manually define the generated data class **`Example`** and the table class **`ExampleTableSO`** to define the connection logic.

#### Example.cs (Data Class Definition)
```csharp
namespace TableData
{
    using System.Collections.Generic;
    using UnityEngine;

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

#### ExampleTableSO.cs (Update Logic Implementation)
```csharp
namespace Table
{
    using TableData;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using TableSO.Scripts;

    public class ExampleTableSO : CustomTableSO<int, Example>
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
            ReleaseData(); // Clear old data

            foreach (var id in ExampleDataTable.GetAllKey())
            {
                List<Sprite> icons = new List<Sprite>();
                
                // Use the IconName (string array from CsvTable) to retrieve Sprite assets from the AssetTable.
                foreach (var iconName in ExampleDataTable.GetData(id).IconName)
                    icons.Add(ExampleIconAssetTable.GetData(iconName).Asset);
                
                // Construct the final Example object and add it to the dataList.
                dataList.Add(new Example(id, icons, ExampleDataTable.GetData(id).Text));
            }

            base.UpdateData(); // Finalize caching
        }

        public override Example GetData(int key)
        {
            // The base implementation provides dictionary lookup. Override for custom logic.
            return base.GetData(key);
        }
    }
}
```
### 4. Runtime Access
The final data can be accessed in your code as follows:
```csharp
// Assume tableCenter is referenced in your script
    var table = tableCenter.GetTable<ExampleTableSO>();
    var data = table.GetData(id); // Retrieve the integrated Example object by ID

    foreach (var icon in data.Icons)
    {
        Image image = GetImage(); // Example function to get a UI Image component
        image.sprite = icon;
    }

    text.text = data.Text;
```

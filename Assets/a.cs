using UnityEngine;
using TableSO.Scripts;
using Table;

public class a : MonoBehaviour
{
    public TableCenter center;

    private void Awake()
    {
        center.Initalize();
        var table = center.GetTable<DamageExpressionDataTableSO>();
    }
}
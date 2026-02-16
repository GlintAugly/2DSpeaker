using System;
using System.Collections.Generic;
using UnityEngine;

public class DataTypeListItemViewResolver : MonoBehaviour
{
    [Serializable]
    public class ViewPrefabEntry
    {
        public ReorderableListDataType DataType;
        public ReorderableListItemView Prefab;
    }

    [SerializeField]
    ViewPrefabEntry[] m_viewPrefabs;

    readonly Dictionary<ReorderableListDataType, ReorderableListItemView> m_prefabMap = new();
    bool m_isBuilt;

    void Awake()
    {
        EnsureMap();
    }

    public bool TryResolvePrefab(IReorderableListItemData itemData, out ReorderableListItemView prefab)
    {
        prefab = null;

        if (itemData == null)
        {
            AppDebug.LogError("ERR: Item data is null.");
            return false;
        }

        EnsureMap();
        if (m_prefabMap.TryGetValue(itemData.DataType, out prefab))
        {
            return true;
        }

        AppDebug.LogError("ERR: No item prefab is configured for data type: {0}", itemData.DataType);
        return false;
    }

    public float GetItemHeight(IReorderableListItemData itemData)
    {
        if (TryResolvePrefab(itemData, out var prefab))
        {
            return prefab.Height;
        }
        return 0f;
    }

    void EnsureMap()
    {
        if (m_isBuilt)
        {
            return;
        }

        m_prefabMap.Clear();
        if (m_viewPrefabs != null)
        {
            for (int i = 0; i < m_viewPrefabs.Length; i++)
            {
                var entry = m_viewPrefabs[i];
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                if (m_prefabMap.ContainsKey(entry.DataType))
                {
                    AppDebug.LogError("ERR: Duplicate ViewPrefabEntry for data type: {0}", entry.DataType);
                    continue;
                }

                m_prefabMap.Add(entry.DataType, entry.Prefab);
            }
        }

        m_isBuilt = true;
    }
}

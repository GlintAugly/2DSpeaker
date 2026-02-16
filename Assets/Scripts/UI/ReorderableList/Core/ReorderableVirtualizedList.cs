using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(DataTypeListItemViewResolver))]
public class ReorderableVirtualizedList : MonoBehaviour
{
    [SerializeField]
    ScrollRect m_scrollRect;
    [SerializeField]
    float m_scrollSensitivity = 30f;
    [SerializeField]
    RectTransform m_viewport;
    [SerializeField]
    RectTransform m_content;
    [SerializeField]
    int m_poolBuffer = 2;
    [SerializeField]
    bool m_isReorderEnabled = true;
    [SerializeField]
    float m_dragAutoScrollEdgeSize = 36f;
    [SerializeField]
    float m_dragAutoScrollSpeed = 0.7f;
    [SerializeField]
    float m_dropIndicatorThickness = 2f;
    [SerializeField]
    float m_itemMargin = 6f;

    readonly List<IReorderableListItemData> m_items = new();
    readonly Dictionary<int, ReorderableListItemView> m_activeViews = new();
    readonly Dictionary<int, Stack<ReorderableListItemView>> m_inactiveViewsByPrefabId = new();
    readonly List<float> m_prefixHeights = new();

    [SerializeField]
    DataTypeListItemViewResolver m_viewResolver;

    int m_selectedIndex = -1;
    int m_dragStartIndex = -1;
    ReorderableListItemView m_dragGhostView;
    ReorderableListItemView m_dragSourceView;
    RectTransform m_dropIndicator;
    int m_dragInsertIndex = -1;
    Canvas m_rootCanvas;
    float m_contentHeight;

    public int SelectedIndex => m_selectedIndex;

    public IReorderableListItemData SelectedItem
    {
        get
        {
            if (m_selectedIndex < 0 || m_selectedIndex >= m_items.Count)
            {
                return null;
            }
            return m_items[m_selectedIndex];
        }
    }

    public bool IsReorderEnabled => m_isReorderEnabled;

    public IReadOnlyList<IReorderableListItemData> Items => m_items;

    public event Action<int> OnSelected;

    public event Action<int, int> OnReordered;

    public event Action<int, IReorderableListItemData> OnSelectedWithData;

    void Awake()
    {
        ResolveReferences();
        RebuildLayoutData();
        if (m_scrollRect != null)
        {
            m_scrollRect.onValueChanged.AddListener(OnScrollChanged);
            m_scrollRect.scrollSensitivity = m_scrollSensitivity;
        }
    }

    void OnDestroy()
    {
        if (m_scrollRect != null)
        {
            m_scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }
    }

    void ResolveReferences()
    {
        if (m_scrollRect == null)
        {
            m_scrollRect = GetComponent<ScrollRect>();
        }
        if (m_scrollRect != null)
        {
            if (m_viewport == null)
            {
                m_viewport = m_scrollRect.viewport;
            }
            if (m_content == null)
            {
                m_content = m_scrollRect.content;
            }
        }

        if (m_viewResolver == null)
        {
            m_viewResolver = GetComponent<DataTypeListItemViewResolver>();
        }
        if (m_viewResolver == null)
        {
            AppDebug.LogError("ERR: DataTypeListItemViewResolver is not configured on {0}", gameObject.name);
        }

        if (m_rootCanvas == null)
        {
            m_rootCanvas = GetComponentInParent<Canvas>();
            if (m_rootCanvas != null)
            {
                m_rootCanvas = m_rootCanvas.rootCanvas;
            }
        }
    }

    public void SetItems(IList<IReorderableListItemData> items, bool preserveScrollPosition = true, bool keepSelectionById = true)
    {
        string selectedId = keepSelectionById ? SelectedItem?.Id : null;
        Vector2 previousScroll = preserveScrollPosition && m_scrollRect != null
            ? m_scrollRect.normalizedPosition
            : new Vector2(0f, 1f);

        m_items.Clear();
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                m_items.Add(items[i]);
            }
        }
        RebuildLayoutData();

        if (!string.IsNullOrEmpty(selectedId))
        {
            m_selectedIndex = FindIndexById(selectedId);
        }
        else if (m_selectedIndex >= m_items.Count)
        {
            m_selectedIndex = -1;
        }

        Refresh(preserveScrollPosition);

        if (preserveScrollPosition && m_scrollRect != null)
        {
            m_scrollRect.normalizedPosition = previousScroll;
            RefreshVisibleRange();
        }
    }

    public T GetSelectedItemAs<T>() where T : class, IReorderableListItemData
    {
        return SelectedItem as T;
    }

    public void Refresh(bool preserveScrollPosition = true)
    {
        Vector2 previousScroll = preserveScrollPosition && m_scrollRect != null
            ? m_scrollRect.normalizedPosition
            : new Vector2(0f, 1f);

        RefreshVisibleRange();

        if (preserveScrollPosition && m_scrollRect != null)
        {
            m_scrollRect.normalizedPosition = previousScroll;
            RefreshVisibleRange();
        }
    }

    public void SetReorderEnabled(bool enabled)
    {
        if (m_isReorderEnabled == enabled)
        {
            return;
        }
        m_isReorderEnabled = enabled;
        RefreshVisibleRange();
    }

    public void OnItemClicked(int index)
    {
        if (!IsValidIndex(index))
        {
            return;
        }

        m_selectedIndex = index;
        RefreshVisibleRange(true);
        OnSelected?.Invoke(index);
        OnSelectedWithData?.Invoke(index, m_items[index]);
    }

    public void OnItemBeginDrag(int index, PointerEventData eventData)
    {
        if (!m_isReorderEnabled || !IsValidIndex(index))
        {
            return;
        }
        m_dragStartIndex = index;
        m_dragInsertIndex = -1;

        m_activeViews.TryGetValue(index, out m_dragSourceView);
        SetViewAlpha(m_dragSourceView, 0.35f);
        CreateDragGhost(index);
        HideDropIndicator();
        UpdateDragGhostPosition(eventData);
    }

    public void OnItemDrag(int index, PointerEventData eventData)
    {
        if (!m_isReorderEnabled || m_dragStartIndex != index)
        {
            return;
        }

        UpdateDragGhostPosition(eventData);
        ProcessDragAutoScroll(eventData);
        m_dragInsertIndex = GetInsertIndexFromPointer(eventData);
        if (m_dragInsertIndex == m_dragStartIndex)
        {
            HideDropIndicator();
        }
        else
        {
            ShowDropIndicator(m_dragInsertIndex);
        }
    }

    public void OnItemEndDrag(int index, PointerEventData eventData)
    {
        if (!m_isReorderEnabled || m_dragStartIndex < 0)
        {
            return;
        }

        int fromIndex = m_dragStartIndex;
        int insertIndex = GetInsertIndexFromPointer(eventData);

        m_dragStartIndex = -1;
        m_dragStartIndex = -1;
        m_dragInsertIndex = -1;
        SetViewAlpha(m_dragSourceView, 1f);
        m_dragSourceView = null;
        DestroyDragGhost();
        HideDropIndicator();

        int toIndex = MoveItemByInsertIndex(fromIndex, insertIndex);
        if (fromIndex >= 0 && toIndex >= 0 && fromIndex != toIndex)
        {
            OnReordered?.Invoke(fromIndex, toIndex);
        }
    }

    int MoveItemByInsertIndex(int fromIndex, int insertIndex)
    {
        if (!IsValidIndex(fromIndex) || insertIndex < 0)
        {
            return -1;
        }

        int originalCount = m_items.Count;
        int normalizedInsertIndex = Mathf.Clamp(insertIndex, 0, originalCount);
        int targetIndex = normalizedInsertIndex;
        if (targetIndex > fromIndex)
        {
            targetIndex--;
        }
        if (targetIndex < 0)
        {
            targetIndex = 0;
        }
        if (targetIndex >= originalCount)
        {
            targetIndex = originalCount - 1;
        }
        if (targetIndex == fromIndex)
        {
            return fromIndex;
        }

        IReorderableListItemData moved = m_items[fromIndex];
        m_items.RemoveAt(fromIndex);
        m_items.Insert(targetIndex, moved);

        if (m_selectedIndex == fromIndex)
        {
            m_selectedIndex = targetIndex;
        }
        else if (fromIndex < m_selectedIndex && targetIndex >= m_selectedIndex)
        {
            m_selectedIndex--;
        }
        else if (fromIndex > m_selectedIndex && targetIndex <= m_selectedIndex)
        {
            m_selectedIndex++;
        }

        RefreshVisibleRange();
        return targetIndex;
    }

    void RebuildLayoutData()
    {
        m_prefixHeights.Clear();
        m_prefixHeights.Add(0f);

        float total = 0f;
        for (int i = 0; i < m_items.Count; i++)
        {
            total += GetItemHeight(i) + m_itemMargin;
            m_prefixHeights.Add(total);
        }

        m_contentHeight = total;
        if (m_content != null)
        {
            Vector2 size = m_content.sizeDelta;
            size.y = total;
            m_content.sizeDelta = size;
        }
    }

    void RefreshVisibleRange(bool selectIndexChanged = false)
    {
        if (m_content == null || m_items.Count == 0)
        {
            ReleaseAllViews();
            return;
        }

        float viewportHeight = GetViewportHeight();
        float scrollTop = GetScrollTop(viewportHeight);
        float scrollBottom = scrollTop + viewportHeight;

        int startIndex = Mathf.Clamp(GetIndexAtY(scrollTop) - m_poolBuffer, 0, m_items.Count - 1);
        int endIndex = Mathf.Clamp(GetIndexAtY(scrollBottom) + m_poolBuffer, 0, m_items.Count - 1);

        ReleaseOutOfRangeViews(startIndex, endIndex);

        for (int index = startIndex; index <= endIndex; index++)
        {
            ReorderableListItemView view = GetOrCreateView(index);
            if (view == null)
            {
                continue;
            }
            bool isSelected = view.IsExpanded;
            if (selectIndexChanged)
            {
                isSelected = (index == m_selectedIndex) && !view.IsExpanded;
            }
            view.Bind(m_items[index], index, isSelected, m_isReorderEnabled);
            PlaceView(view, index);
        }
        RebuildLayoutData();
        for (int index = startIndex; index <= endIndex; index++)
        {
            if (GetView(index) is ReorderableListItemView view)
            {
                PlaceView(view, index);
            }
        }
    }

    void PlaceView(ReorderableListItemView view, int index)
    {
        float y = m_prefixHeights[index];
        float height = GetItemHeight(index);

        RectTransform root = view.Root;
        root.SetParent(m_content, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -y);
        root.sizeDelta = new Vector2(0f, height);
    }

    void ReleaseOutOfRangeViews(int startIndex, int endIndex)
    {
        if (m_activeViews.Count == 0)
        {
            return;
        }

        List<int> keys = new List<int>(m_activeViews.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int index = keys[i];
            if (index >= startIndex && index <= endIndex || index == m_dragStartIndex)
            {
                continue;
            }

            ReorderableListItemView view = m_activeViews[index];
            m_activeViews.Remove(index);
            view.gameObject.SetActive(false);
            GetInactiveStack(view.PoolPrefabId).Push(view);
        }
    }

    ReorderableListItemView GetView(int dataIndex)
    {
        if (m_activeViews.TryGetValue(dataIndex, out var view))
        {
            return view;
        }
        return null;
    }
    ReorderableListItemView GetOrCreateView(int index)
    {
        if (m_activeViews.TryGetValue(index, out var existing))
        {
            return existing;
        }

        if (m_viewResolver == null || !IsValidIndex(index))
        {
            return null;
        }

        IReorderableListItemData data = m_items[index];
        if (!m_viewResolver.TryResolvePrefab(data, out var prefab) || prefab == null)
        {
            return null;
        }

        int prefabId = prefab.GetInstanceID();
        Stack<ReorderableListItemView> pool = GetInactiveStack(prefabId);
        ReorderableListItemView view;
        if (pool.Count > 0)
        {
            view = pool.Pop();
        }
        else
        {
            view = Instantiate(prefab, m_content);
            view.Initialize(this);
        }

        view.PoolPrefabId = prefabId;

        view.gameObject.SetActive(true);
        m_activeViews[index] = view;
        return view;
    }

    void ReleaseAllViews()
    {
        if (m_activeViews.Count == 0)
        {
            return;
        }

        foreach (var pair in m_activeViews)
        {
            pair.Value.gameObject.SetActive(false);
            GetInactiveStack(pair.Value.PoolPrefabId).Push(pair.Value);
        }
        m_activeViews.Clear();
    }

    Stack<ReorderableListItemView> GetInactiveStack(int prefabId)
    {
        if (!m_inactiveViewsByPrefabId.TryGetValue(prefabId, out var stack))
        {
            stack = new Stack<ReorderableListItemView>();
            m_inactiveViewsByPrefabId[prefabId] = stack;
        }
        return stack;
    }

    void CreateDragGhost(int index)
    {
        DestroyDragGhost();

        if (m_rootCanvas == null || !IsValidIndex(index) || m_viewResolver == null)
        {
            return;
        }

        if (!m_viewResolver.TryResolvePrefab(m_items[index], out var prefab) || prefab == null)
        {
            return;
        }

        m_dragGhostView = Instantiate(prefab, this.transform);
        m_dragGhostView.Initialize(this);
        m_dragGhostView.Bind(m_items[index], index, true, false);

        if (!m_dragGhostView.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup = m_dragGhostView.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.9f;

        RectTransform ghostRoot = m_dragGhostView.Root;
        ghostRoot.SetAsLastSibling();
    }

    void DestroyDragGhost()
    {
        if (m_dragGhostView != null)
        {
            Destroy(m_dragGhostView.gameObject);
            m_dragGhostView = null;
        }
    }

    void EnsureDropIndicator()
    {
        if (m_dropIndicator != null || m_viewport == null || m_viewport.childCount == 0)
        {
            return;
        }

        GameObject indicator = new("DropIndicator", typeof(RectTransform), typeof(Image));
        indicator.transform.SetParent(m_viewport.GetChild(0), false);
        m_dropIndicator = indicator.GetComponent<RectTransform>();

        Image image = indicator.GetComponent<Image>();
        image.raycastTarget = false;

        m_dropIndicator.anchorMin = new Vector2(0f, 1f);
        m_dropIndicator.anchorMax = new Vector2(1f, 1f);
        m_dropIndicator.pivot = new Vector2(0.5f, 0.5f);
        m_dropIndicator.gameObject.SetActive(false);
    }

    void ShowDropIndicator(int insertIndex)
    {
        if (insertIndex < 0)
        {
            HideDropIndicator();
            return;
        }

        EnsureDropIndicator();
        if (m_dropIndicator == null)
        {
            return;
        }

        int clampedInsert = Mathf.Clamp(insertIndex, 0, m_items.Count);
        float y = clampedInsert <= 0 ? 0f : m_prefixHeights[Mathf.Min(clampedInsert, m_prefixHeights.Count - 1)] - m_itemMargin * 0.5f;

        m_dropIndicator.anchoredPosition = new Vector2(0f, -y);
        m_dropIndicator.sizeDelta = new Vector2(0f, Mathf.Max(1f, m_dropIndicatorThickness));
        m_dropIndicator.SetAsLastSibling();
        m_dropIndicator.gameObject.SetActive(true);
    }

    void HideDropIndicator()
    {
        if (m_dropIndicator != null)
        {
            m_dropIndicator.gameObject.SetActive(false);
        }
    }

    bool ScreenPointToLocalPointWhenDragging(PointerEventData eventData, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (m_content == null || m_viewport == null || eventData == null)
        {
            return false;
        }

        Camera eventCamera = m_rootCanvas != null && m_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(m_viewport, eventData.position, eventCamera, out localPoint);
    }

    void UpdateDragGhostPosition(PointerEventData eventData)
    {
        if (m_dragGhostView == null || m_dragGhostView.Root == null)
        {
            return;
        }
        if (ScreenPointToLocalPointWhenDragging(eventData, out var localPoint))
        {
            m_dragGhostView.Root.anchoredPosition = localPoint;
        }
    }

    void ProcessDragAutoScroll(PointerEventData eventData)
    {
        if (m_dragAutoScrollSpeed <= 0f)
        {
            return;
        }

        if (!ScreenPointToLocalPointWhenDragging(eventData, out var localPoint))
        {
            return;
        }

        Rect rect = m_viewport.rect;
        float delta = 0f;
        float edge = Mathf.Max(1f, m_dragAutoScrollEdgeSize);

        if (localPoint.y > rect.yMax - edge)
        {
            float t = Mathf.InverseLerp(rect.yMax - edge, rect.yMax, localPoint.y);
            delta = m_dragAutoScrollSpeed * t * Time.unscaledDeltaTime;
        }
        else if (localPoint.y < rect.yMin + edge)
        {
            float t = Mathf.InverseLerp(rect.yMin + edge, rect.yMin, localPoint.y);
            delta = -m_dragAutoScrollSpeed * t * Time.unscaledDeltaTime;
        }

        if (Mathf.Abs(delta) <= 0f)
        {
            return;
        }

        m_scrollRect.verticalNormalizedPosition = Mathf.Clamp01(m_scrollRect.verticalNormalizedPosition + delta);
        RefreshVisibleRange();
    }

    void SetViewAlpha(ReorderableListItemView view, float alpha)
    {
        if (view == null)
        {
            return;
        }

        CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = alpha;
    }

    float GetItemHeight(int index)
    {
        if (GetView(index) is ReorderableListItemView view)
        {
            return view.Height;
        }
        if (m_viewResolver != null && IsValidIndex(index))
        {
            return m_viewResolver.GetItemHeight(m_items[index]);
        }
        return 0f;
    }

    float GetViewportHeight()
    {
        if (m_viewport == null)
        {
            return 0f;
        }
        return m_viewport.rect.height;
    }

    float GetScrollTop(float viewportHeight)
    {
        if (m_scrollRect == null)
        {
            return 0f;
        }

        float maxOffset = Mathf.Max(0f, m_contentHeight - viewportHeight);
        return (1f - m_scrollRect.verticalNormalizedPosition) * maxOffset;
    }

    int GetIndexAtY(float yFromTop)
    {
        if (m_items.Count == 0)
        {
            return -1;
        }

        float y = Mathf.Clamp(yFromTop, 0f, Mathf.Max(0f, m_contentHeight - 0.001f));
        int low = 0;
        int high = m_items.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            float start = m_prefixHeights[mid];
            float end = m_prefixHeights[mid + 1];

            if (y < start)
            {
                high = mid - 1;
            }
            else if (y >= end)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }

        return Mathf.Clamp(low, 0, m_items.Count - 1);
    }

    int GetInsertIndexFromPointer(PointerEventData eventData)
    {
        if (m_items.Count == 0 || eventData == null)
        {
            return -1;
        }

        if (!ScreenPointToLocalPointWhenDragging(eventData, out var localPoint))
        {
            return -1;
        }

        if (localPoint.y > 0f || localPoint.y < -m_viewport.rect.height)
        {
            return -1;
        }

        float yFromTop = GetScrollTop(GetViewportHeight()) - localPoint.y;

        int index = GetIndexAtY(yFromTop);
        if (index < 0)
        {
            return -1;
        }

        float start = m_prefixHeights[index];
        float end = m_prefixHeights[index + 1];
        float mid = (start + end) * 0.5f;
        return yFromTop < mid ? index : index + 1;
    }

    int FindIndexById(string id)
    {
        for (int i = 0; i < m_items.Count; i++)
        {
            if (m_items[i] != null && m_items[i].Id == id)
            {
                return i;
            }
        }
        return -1;
    }

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < m_items.Count;
    }

    void OnScrollChanged(Vector2 _)
    {
        RefreshVisibleRange();
    }
}

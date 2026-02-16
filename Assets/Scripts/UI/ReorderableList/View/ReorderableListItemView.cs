using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(IReorderableListItemPresenter), typeof(RectTransform))]
public class ReorderableListItemView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    RectTransform m_root;
    [SerializeField]
    LayoutElement m_layoutElement;
    [SerializeField]
    RectTransform m_compactRoot;
    [SerializeField]
    RectTransform m_expandedRoot;
    
    IReorderableListItemPresenter m_itemPresenter;

    ReorderableVirtualizedList m_owner;
    int m_boundIndex = -1;
    bool m_isReorderEnabled;
    int m_poolPrefabId;

    public int BoundIndex => m_boundIndex;
    public int PoolPrefabId
    {
        get => m_poolPrefabId;
        set => m_poolPrefabId = value;
    }

    public RectTransform Root
    {
        get
        {
            if (m_root == null)
            {
                m_root = transform as RectTransform;
            }
            return m_root;
        }
    }

    public bool IsExpanded
    {
        get
        {
            if (m_expandedRoot != null)
            {
                return m_expandedRoot.gameObject.activeSelf;
            }
            return false;
        }
    }

    public float Height
    {
        get
        {
            if(m_expandedRoot != null && m_expandedRoot.gameObject.activeSelf)
            {
                return m_expandedRoot.rect.height;
            }
            else if(m_compactRoot != null && m_compactRoot.gameObject.activeSelf)
            {
                return m_compactRoot.rect.height;
            }
            return Root.rect.height;
        }
    }
    public void Initialize(ReorderableVirtualizedList owner)
    {
        m_owner = owner;
        ResolvePresenter();
    }

    void Awake()
    {
        ResolvePresenter();
    }

    void OnValidate()
    {
        ResolvePresenter();
    }

    void ResolvePresenter()
    {
        m_itemPresenter = GetComponent<IReorderableListItemPresenter>();
        if (m_itemPresenter == null)
        {
            AppDebug.LogError("ERR: IReorderableListItemPresenter is not found on {0}", gameObject.name);
        }
    }

    public void Bind(IReorderableListItemData data, int index, bool isSelected, bool reorderEnabled)
    {
        m_boundIndex = index;
        m_isReorderEnabled = reorderEnabled;

        m_itemPresenter.Bind(data, isSelected);

        bool hasExpandedRoot = m_expandedRoot != null;
        bool showExpanded = isSelected && hasExpandedRoot;
        bool showCompact = !showExpanded;

        if (m_compactRoot != null)
        {
            m_compactRoot.gameObject.SetActive(showCompact);
        }
        if (hasExpandedRoot)
        {
            m_expandedRoot.gameObject.SetActive(showExpanded);
        }
        
        if (m_layoutElement != null)
        {
            m_layoutElement.preferredHeight = this.Height;
            m_layoutElement.minHeight = this.Height;
        }

        var root = Root;
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_owner == null || m_boundIndex < 0)
        {
            return;
        }
        m_owner.OnItemClicked(m_boundIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_owner == null || !m_isReorderEnabled || m_boundIndex < 0)
        {
            return;
        }
        m_owner.OnItemBeginDrag(m_boundIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_owner == null || !m_isReorderEnabled || m_boundIndex < 0)
        {
            return;
        }
        m_owner.OnItemDrag(m_boundIndex, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (m_owner == null || !m_isReorderEnabled || m_boundIndex < 0)
        {
            return;
        }
        m_owner.OnItemEndDrag(m_boundIndex, eventData);
    }
}

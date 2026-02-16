using UnityEngine;
using UnityEngine.UI;

public class DefaultListItemPresenter : MonoBehaviour, IReorderableListItemPresenter
{
    [SerializeField]
    Text m_titleText;
    [SerializeField]
    Text m_summaryText;
    [SerializeField]
    Text m_detailText;

    public void Bind(IReorderableListItemData data, bool isSelected)
    {
        string title = string.Empty;
        string summary = string.Empty;
        string detail = string.Empty;

        if (data is ReorderableListItemData normal)
        {
            title = normal.Title;
            summary = normal.Summary;
            detail = normal.Detail;
        }
        else if (data is StringListItemData stringData)
        {
            title = stringData.Value;
            summary = stringData.Value;
            detail = string.Empty;
        }
        else if (data is JsonCommandListItemData commandData)
        {
            title = commandData.Command != null ? commandData.Command.name : string.Empty;
            summary = commandData.Summary;
            detail = commandData.Detail;
        }

        if (m_titleText != null)
        {
            m_titleText.text = title;
        }
        if (m_summaryText != null)
        {
            m_summaryText.text = summary;
        }
        if (m_detailText != null)
        {
            m_detailText.text = detail;
        }
    }
}

using System;

[Serializable]
public class ReorderableListItemData : IReorderableListItemData
{
    public string Id;
    public ReorderableListDataType DataType = ReorderableListDataType.Default;
    public string Title;
    public string Summary;
    public string Detail;

    string IReorderableListItemData.Id => Id;
    ReorderableListDataType IReorderableListItemData.DataType => DataType;
}

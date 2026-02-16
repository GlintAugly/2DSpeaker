using System;

[Serializable]
public class StringListItemData : IReorderableListItemData
{
    public string Id;
    public string Value;
    public ReorderableListDataType DataType = ReorderableListDataType.PlainText;

    string IReorderableListItemData.Id => Id;
    ReorderableListDataType IReorderableListItemData.DataType => DataType;
}

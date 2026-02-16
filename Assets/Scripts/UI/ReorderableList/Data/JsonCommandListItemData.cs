using System;
using System.Text;

[Serializable]
public class JsonCommandListItemData : IReorderableListItemData
{
    public string Id;
    public ReorderableListDataType DataType = ReorderableListDataType.JsonCommand;
    public JsonCommandData Command;

    public string Summary
    {
        get
        {
            int maxPreview = 40;
            string fullcommand = GetCommandDetail();
            if (fullcommand.Length > maxPreview)
            {
                return fullcommand[..maxPreview] + " ...";
            }

            return fullcommand;
        }
    }

    public string Detail => GetCommandDetail();

    string GetCommandDetail()
    {
        if (Command == null)
        {
            return string.Empty;
        }

        if (Command.paramArray == null || Command.paramArray.Length == 0)
        {
            return Command.name;
        }

        StringBuilder sb = new();
        sb.Append(Command.name);
        sb.Append(" : ");

        int maxPreview = 2;
        int count = Math.Min(maxPreview, Command.paramArray.Length);
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            JsonParameterValue param = Command.paramArray[i];
            if (param == null)
            {
                continue;
            }

            sb.Append(param.key);
            sb.Append('=');
            sb.Append(param.value);
        }
        return sb.ToString();
    }
    string IReorderableListItemData.Id => Id;
    ReorderableListDataType IReorderableListItemData.DataType => DataType;
}

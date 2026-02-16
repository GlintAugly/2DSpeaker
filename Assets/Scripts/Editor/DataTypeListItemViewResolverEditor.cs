using UnityEditor;

[CustomEditor(typeof(DataTypeListItemViewResolver))]
[CanEditMultipleObjects]
public class DataTypeListItemViewResolverEditor : Editor
{
    SerializedProperty m_viewPrefabsProperty;

    void OnEnable()
    {
        m_viewPrefabsProperty = serializedObject.FindProperty("m_viewPrefabs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((DataTypeListItemViewResolver)target), typeof(DataTypeListItemViewResolver), false);
        }

        if (m_viewPrefabsProperty != null)
        {
            EditorGUILayout.PropertyField(m_viewPrefabsProperty, includeChildren: true);
        }
        else
        {
            EditorGUILayout.HelpBox("m_viewPrefabs が見つかりません。", MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

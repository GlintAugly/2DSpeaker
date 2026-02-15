using UnityEngine;

public interface IEditController
{
    public EditorController EditorController { set; }
    public GameObject gameObject { get; }
}

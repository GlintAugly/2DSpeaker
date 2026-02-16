using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // シーン内から既存のインスタンスを検索
                instance = FindFirstObjectByType<T>();

                // 見つからない場合は新規作成
                if (instance == null)
                {
                    GameObject singletonObject = new(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                    if (singletonObject.scene.name != "DontDestroyOnLoad")
                    {
                        DontDestroyOnLoad(singletonObject);
                    }
                }
            }
            return instance;
        }
    }

    public static bool IsInitialized => instance != null;

    protected virtual void Awake()
    {
        // スレッドセーフ性を高める
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return; // 早期終了で二重処理を防ぐ
        }
        
        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}

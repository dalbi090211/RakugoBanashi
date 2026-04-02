using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();

                if (instance == null)
                    Debug.LogWarning($"{typeof(T).Name} instance not found.");
            }

            return instance;
        }
    }

    public static bool IsValid => instance != null;

    protected virtual void Awake()
    {
        T current = this as T;

        if (instance != null && instance != current)
        {
            Destroy(gameObject);
            return;
        }

        instance = current;
        DontDestroyOnLoad(gameObject);
    }
}
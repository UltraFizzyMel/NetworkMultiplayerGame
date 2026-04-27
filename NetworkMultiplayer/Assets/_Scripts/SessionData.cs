using UnityEngine;

public class SessionData : MonoBehaviour
{
    public static SessionData Instance;

    public bool isHostDeck;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

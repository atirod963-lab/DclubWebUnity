using UnityEngine;

public class OrientationManager : MonoBehaviour
{
    public static OrientationManager instance;
    public GameObject warningPanel;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Screen.width > Screen.height)
        {
            if (!warningPanel.activeSelf) warningPanel.SetActive(true);
        }
        else
        {
            if (warningPanel.activeSelf) warningPanel.SetActive(false);
        }
    }
}
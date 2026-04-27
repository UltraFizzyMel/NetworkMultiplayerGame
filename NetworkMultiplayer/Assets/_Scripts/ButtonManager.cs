using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    public void LoadLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void Quit()
    {
        #if UNITY_EDITOR
                // This stops Play Mode in the Unity Editor
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                // This closes the actual built application
                Application.Quit();
        #endif
    }
}

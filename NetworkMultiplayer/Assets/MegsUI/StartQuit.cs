using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartQuit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Quit()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MainMenu"); 
     
    }

    public void Lobby()
        {
        SceneManager.LoadScene("Lobby");
    }
}

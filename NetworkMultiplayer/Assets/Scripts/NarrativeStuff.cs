using UnityEngine;
using UnityEngine.SceneManagement;

public class NarrativeStuff : MonoBehaviour
{
    public GameObject Image;
    public GameObject Image2;

    public void DisableImage()
    {
        Image.SetActive(false);
        Image2.SetActive(true);
    }

    public void NextScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

}

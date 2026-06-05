using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject howToPanel;

    [SerializeField] private List<GameObject> pnlTutorial = new List<GameObject>();

    [SerializeField] private bool isGame = false;

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

    public void ShowControls()
    {
        controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
    }

    public void ShowHowTo()
    {
        ShowIntro();
        howToPanel.SetActive(true);
    }

    public void ShowIntro()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject introPanel = pnlTutorial[0];
        introPanel.SetActive(true);
    }

    public void ShowLeaks()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject leaksPanel = pnlTutorial[1];
        leaksPanel.SetActive(true);
    }

    public void ShowBucket()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject bucketPanel = pnlTutorial[2];
        bucketPanel.SetActive(true);
    }

    public void ShowGenerator()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject generatorPanel = pnlTutorial[3];
        generatorPanel.SetActive(true);
    }

    public void ShowSteering()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject steeringPanel = pnlTutorial[4];
        steeringPanel.SetActive(true);
    }

    public void ShowSwap()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject swapPanel = pnlTutorial[5];
        swapPanel.SetActive(true);
    }

    public void ShowWin()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject winPanel = pnlTutorial[6];
        winPanel.SetActive(true);
    }

    public void CloseHowTo()
    {
        howToPanel.SetActive(false);
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

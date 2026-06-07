using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject howToPanel;

    [SerializeField] private List<GameObject> pnlTutorial = new List<GameObject>();
    [SerializeField] private List<GameObject> imgTutButton = new List<GameObject>();

    [SerializeField] private GameObject tutorialPanel;

    [SerializeField] private GameObject btnBack;

    [SerializeField] private bool isGame = false;
    private bool _tutorialComplete;
    private bool _gameStarted;

    [SerializeField] private GameObject btnSkip;

    [SerializeField] private AudioClip lobbySong;
    [SerializeField] private AudioClip menuSong;

    [SerializeField] private bool isEnd;
    [SerializeField] private bool hasLost;

    public void Start()
    {
        if (isGame)
            ShowIntro();

        if (isEnd)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void PlayClickSound()
    {
        MusicManager.Instance.PlaySFX(SFXType.Click);
    }

    public void LoadLobby()
    {
        MusicManager.Instance.PlaySFX(SFXType.Start);
        MusicManager.Instance.CrossfadeToNewSong(lobbySong, "Ambient Song", 0.2f, 0.05f);
        SceneManager.LoadScene("Lobby");
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
        PlayClickSound();
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        PlayClickSound();
    }

    public void ShowControls()
    {
        controlsPanel.SetActive(true);
        PlayClickSound();
    }

    public void CloseControls()
    {
        controlsPanel.SetActive(false);
        PlayClickSound();
    }

    public void ShowHowTo()
    {
        ShowIntro();
        howToPanel.SetActive(true);
        PlayClickSound();
    }

    public void ShowIntro()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);
                
        GameObject introPanel = pnlTutorial[0];
        introPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgIntro = imgTutButton[0];
        imgIntro.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowLeaks()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject leaksPanel = pnlTutorial[1];
        leaksPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgLeaks = imgTutButton[1];
        imgLeaks.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowBucket()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject bucketPanel = pnlTutorial[2];
        bucketPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgBucket = imgTutButton[2];
        imgBucket.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowGenerator()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject generatorPanel = pnlTutorial[3];
        generatorPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgGenerator = imgTutButton[3];
        imgGenerator.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowSteering()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject steeringPanel = pnlTutorial[4];
        steeringPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgSteering = imgTutButton[4];
        imgSteering.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowSwap()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject swapPanel = pnlTutorial[5];
        swapPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgSwap = imgTutButton[5];
        imgSwap.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(true);
    }

    public void ShowWin()
    {
        foreach (GameObject panel in pnlTutorial)
            panel.SetActive(false);

        GameObject winPanel = pnlTutorial[6];
        winPanel.SetActive(true);

        foreach (GameObject button in imgTutButton)
            button.SetActive(false);

        GameObject imgWin = imgTutButton[6];
        imgWin.SetActive(true);

        PlayClickSound();

        if (isGame)
            btnSkip.SetActive(false);
    }

    public void FinishTutorial()
    {
        if (_tutorialComplete) return; // Prevent double-calls from button spam
        _tutorialComplete = true;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (GameManager.Instance == null) return;

        // If the other player already finished, GameReady will be true
        // the moment our RPC resolves — no need to show the waiting panel.
        // If not, show it and Update() will hide it when ready.
        if (!GameManager.Instance.GameReady() && GameManager.Instance.LoadingOverlay != null)
            GameManager.Instance.LoadingOverlay.SetActive(true);

        GameManager.Instance.ReportTutorialComplete();
    }

    public void ShowBtnBack()
    {
        btnBack.SetActive(true);
    }

    public void CloseHowTo()
    {
        howToPanel.SetActive(false);
        PlayClickSound();
    }

    public void LoadMainMenu()
    {
        PlayClickSound();

        if (isEnd)
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

        if (hasLost)
            MusicManager.Instance.CrossfadeToNewSong(menuSong, "Menu Song", 0.7f, 0.2f);
        else
            MusicManager.Instance.CrossfadeToNewSong(menuSong, "Menu Song", 0.05f, 0.2f);

        SceneManager.LoadScene("MainMenu");
    }

    public void Quit()
    {
        PlayClickSound();

        #if UNITY_EDITOR
                // This stops Play Mode in the Unity Editor
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                // This closes the actual built application
                Application.Quit();
        #endif
    }
}

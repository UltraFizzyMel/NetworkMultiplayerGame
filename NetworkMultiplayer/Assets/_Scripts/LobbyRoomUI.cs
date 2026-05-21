using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyRoomUI : MonoBehaviour
{
    /*public static LobbyRoomUI Instance;

    [SerializeField] private GameObject lobbyRoomPanel;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    //[SerializeField] private Transform playerListParent;

    public AudioClip radioStaticSong;
    public AudioClip ambientSong;

    [SerializeField] private Button btnReadyOne;
    [SerializeField] private Button btnReadyTwo;
    [SerializeField] private TextMeshProUGUI txtReadyOne;
    [SerializeField] private TextMeshProUGUI txtReadyTwo;
    [SerializeField] private GameObject imgDeckOne;
    [SerializeField] private GameObject imgDeckTwo;
    [SerializeField] private GameObject imgCabinOne;
    [SerializeField] private GameObject imgCabinTwo;

    [SerializeField] CanvasGroup pnlPlayerTwo;

    [SerializeField] private TextMeshProUGUI txtLobbyName;
    //[SerializeField] private Button startGameButton;
    [SerializeField] private Button btnLeave;

    //[SerializeField] private bool canInteract;
    [SerializeField] private bool isReady = false;
    [SerializeField] private bool isHost;
    [SerializeField] private GameObject txtPlayerTwo;
    [SerializeField] private GameObject txtWaiting;

    [SerializeField] private GameObject lobbyListParent;

    private Lobby currentLobby;

    private bool isUpdatingLobby = false;
    private bool hasUpdatedVisuals = false;

    private bool shouldStopUpdateLoop = false;

    // Scene pre-loading
    private AsyncOperation preloadOperation;
    private bool isScenePreloaded = false;

    private void Awake()
    {
        Instance = this;

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    public void OpenLobbyRoom(Lobby lobby, bool isDeck)//, Unity.Services.Lobbies.Models.Player player)
    {
        currentLobby = lobby;
        lobbyListParent.SetActive(false);
        lobbyRoomPanel.SetActive(true);

        // Start pre-loading the game scene immediately
        StartCoroutine(PreloadGameScene());

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(radioStaticSong, "RadioStatic");

        string myID = AuthenticationService.Instance.PlayerId;
        isHost = currentLobby.HostId == myID;

        txtLobbyName.text = currentLobby.Name;

        // Default UI state
        txtReadyOne.text = "Not Ready";
        txtReadyTwo.text = "Not Ready";

        btnReadyOne.GetComponent<Image>().color = Color.red;
        btnReadyTwo.GetComponent<Image>().color = Color.red;

        imgDeckOne.SetActive(false);
        imgDeckTwo.SetActive(false);
        imgCabinOne.SetActive(false);
        imgCabinTwo.SetActive(false);

        if (isHost)
        {
            pnlPlayerTwo.alpha = 0.5f;
            txtPlayerTwo.SetActive(false);
            txtWaiting.SetActive(true);
            if (isDeck)
            {
                SessionData.Instance.isHostDeck = true;
                imgDeckOne.SetActive(true);
                imgCabinTwo.SetActive(true);
            }
            else
            {
                SessionData.Instance.isHostDeck = false;
                imgCabinOne.SetActive(true);
                imgDeckTwo.SetActive(true);
            }
        }
        else
        {
            if (isDeck)
            {
                SessionData.Instance.isHostDeck = false;
                imgDeckTwo.SetActive(true);
                imgCabinOne.SetActive(true);
            }
            else
            {
                SessionData.Instance.isHostDeck = true;
                imgCabinTwo.SetActive(true);
                imgDeckOne.SetActive(true);
            }
        }

        RefreshUI();

        if (!isUpdatingLobby)
        {
            UpdateLobbyLoop();
        }

        //UpdateUI();
    }

    private IEnumerator PreloadGameScene()
    {
        if (isScenePreloaded)
        {
            Debug.Log("GameScene already preloaded");
            yield break;
        }

        Debug.Log("Starting to preload GameScene...");

        // Check if scene is in build settings
        if (!CanLoad("GameScene"))
        {
            Debug.LogError("GameScene is not in build settings! Cannot preload.");
            yield break;
        }

        preloadOperation = SceneManager.LoadSceneAsync("GameScene");

        if (preloadOperation == null)
        {
            Debug.LogError("Failed to start async scene load!");
            yield break;
        }

        // Don't activate the scene immediately
        preloadOperation.allowSceneActivation = false;

        // Wait until the scene is 90% loaded
        while (preloadOperation.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(preloadOperation.progress / 0.9f);
            Debug.Log($"Preloading GameScene: {progress * 100:F0}%");
            yield return null;
        }

        isScenePreloaded = true;
        Debug.Log("GameScene preloaded and ready! Waiting for activation.");
    }

    public async void ToggleReady()
    {
        if (currentLobby == null)
        {
            Debug.LogError("Current lobby is null!");
            return;
        }

        isReady = !isReady;
        string newState = isReady ? "true" : "false";

        Debug.Log($"Toggling ready state to: {isReady} for player: {AuthenticationService.Instance.PlayerId}");

        var data = new Dictionary<string, PlayerDataObject>
        {
            {
                "Ready",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newState)
            }
        };
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions { Data = data }
            );

            await Task.Delay(500);
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            Debug.Log($"Lobby refreshed. Players: {currentLobby.Players.Count}");

            foreach (var player in currentLobby.Players)
            {
                bool playerReady = player.Data != null &&
                                  player.Data.ContainsKey("Ready") &&
                                  player.Data["Ready"].Value == "true";
                Debug.Log($"Player {player.Id}: Ready = {playerReady}");
            }

            if (AllPlayersReady())
            {
                Debug.Log("All players are ready! Starting game...");
                //NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
                if (isHost)
                {
                    Debug.Log("Host is starting the game...");
                    StartGameScene();
                }
            }

            RefreshUI();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to update ready state: " + e.Message);
        }
    }

    private bool AllPlayersReady()
    {
        if (currentLobby == null)
        {
            Debug.Log("AllPlayersReady: currentLobby is null");
            return false;
        }

        if (currentLobby.Players == null)
        {
            Debug.Log("AllPlayersReady: Players list is null");
            return false;
        }

        Debug.Log($"AllPlayersReady: Checking {currentLobby.Players.Count} players");

        if (currentLobby.Players.Count < 2)
        {
            Debug.Log("AllPlayersReady: Not enough players (need 2)");
            return false;
        }

        foreach (Unity.Services.Lobbies.Models.Player player in currentLobby.Players)
        {
            if (player.Data == null)
            {
                Debug.Log($"AllPlayersReady: Player {player.Id} has no data");
                return false;
            }

            // Check if data contains "Ready" key
            if (!player.Data.ContainsKey("Ready"))
            {
                Debug.Log($"AllPlayersReady: Player {player.Id} has no 'Ready' key");
                return false;
            }

            // Check if ready value is "true"
            bool isReady = player.Data["Ready"].Value == "true";
            Debug.Log($"AllPlayersReady: Player {player.Id} Ready = {isReady} (Value: '{player.Data["Ready"].Value}')");

            if (!isReady)
            {
                return false;
            }
        }

        Debug.Log("AllPlayersReady: ALL PLAYERS ARE READY!");
        return true;
    }

    private void StartGameScene()
    {
        // Make sure NetworkManager is running
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null! Cannot load scene.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Only the host can load scenes! Current mode: " +
                          (NetworkManager.Singleton.IsHost ? "Host" :
                           NetworkManager.Singleton.IsServer ? "Server" :
                           NetworkManager.Singleton.IsClient ? "Client" : "None"));
            return;
        }

        // Check if the scene is added to build settings
        string sceneName = "GameScene";
        if (CanLoad(sceneName))
        {
            Debug.Log($"Loading scene: {sceneName}");

            StopAllBackgroundProcesses();

            // Load the scene - this will automatically sync to all clients
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "AmbientEnvironment");
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' is not in the build settings! " +
                          "Add it in File > Build Settings > Scenes in Build");
        }

        /*if (!CanLoad(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in the build settings!");
            return;
        }

        Debug.Log($"Starting game scene transition...");

        // Show loading screen immediately
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (loadingBar != null)
                loadingBar.value = 0f;
        }*/

        /*StopAllBackgroundProcesses();

        // If scene is preloaded, activate it (much faster)
        if (isScenePreloaded && preloadOperation != null)
        {
            Debug.Log("Activating preloaded scene...");
            StartCoroutine(ActivatePreloadedScene());
        }
        else
        {
            Debug.Log("Scene not preloaded, loading now...");
            // Fallback to regular network scene loading
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        // Change music (will play once the new scene loads)
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "AmbientEnvironment");
        }
    }

    private IEnumerator ActivatePreloadedScene()
    {
        // Update loading bar to show progress
        if (loadingBar != null)
            loadingBar.value = 0.8f;

        // Small delay to show the loading screen
        yield return new WaitForSeconds(0.3f);

        // Tell NetworkManager to load the scene (this syncs with clients)
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);

        // Alternatively, if you want to avoid NetworkManager scene loading for some reason:
        // preloadOperation.allowSceneActivation = true;

        // Wait for scene activation
        while (!preloadOperation.isDone)
        {
            if (loadingBar != null)
                loadingBar.value = 0.8f + (preloadOperation.progress * 0.2f);
            yield return null;
        }

        // Scene is now active
        isScenePreloaded = false;
        preloadOperation = null;
    }

    // Helper method to check if a scene can be loaded
    private bool CanLoad(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    private void StopAllBackgroundProcesses()
    {
        Debug.Log("Stopping all background processes...");

        // Stop the update loop
        shouldStopUpdateLoop = true;
        isUpdatingLobby = false;

        // Stop the lobby manager processes
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.StopAllLobbyProcesses();
        }

        // Clean up references
        currentLobby = null;
    }

    private async void UpdateLobbyLoop()
    {
        if (isUpdatingLobby) return;
        isUpdatingLobby = true;

        while (!shouldStopUpdateLoop && lobbyRoomPanel != null && lobbyRoomPanel.activeInHierarchy)
        {
            await Task.Delay(15000);

            if (currentLobby == null) break;

            try
            {
                var oldLobby = currentLobby;
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                if (currentLobby == null)
                {
                    Debug.Log("Lobby is null, game might be starting...");
                    break;
                }

                if (isHost && AllPlayersReady())
                {
                    Debug.Log("UpdateLobbyLoop: All players ready, starting game...");
                    StartGameScene();
                    break; // Exit the loop once we start loading
                }

                RefreshUI();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"Lobby refresh failed: {e.Message}");

                // If lobby not found, it might have been deleted because game started
                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    Debug.Log("Lobby not found during refresh. Game might be starting...");
                    break;
                }
            }
        }

        isUpdatingLobby = false;
    }

    private void RefreshUI()
    {
        if (this == null || btnReadyOne == null || btnReadyTwo == null)
            return;

        if (currentLobby == null) return;

        bool hasTwoPlayers = currentLobby.Players.Count >= 2;

        // Default: disable both
        btnReadyOne.interactable = false;
        btnReadyTwo.interactable = false;

        if (hasTwoPlayers)
        {
            if (isHost)
            {
                btnReadyOne.interactable = true;
                if (!hasUpdatedVisuals)
                {
                    UpdateVisuals();
                }
            }
            else
            {
                btnReadyTwo.interactable = true;
            }
        }
        else
        {
            ChangeVisualsBack();
        }

            // Update ready text for both players
            for (int i = 0; i < currentLobby.Players.Count; i++)
            {
                if (i >= 2) break;

                Unity.Services.Lobbies.Models.Player player = currentLobby.Players[i];

                bool ready =
                    player.Data != null &&
                    player.Data.ContainsKey("Ready") &&
                    player.Data["Ready"].Value == "true";

                if (i == 0)
                {
                    txtReadyOne.text = ready ? "Ready!" : "Not Ready";
                    btnReadyOne.GetComponent<Image>().color =
                        ready ? Color.green : Color.red;
                }
                else if (i == 1)
                {
                    txtReadyTwo.text = ready ? "Ready!" : "Not Ready";
                    btnReadyTwo.GetComponent<Image>().color =
                        ready ? Color.green : Color.red;
                }
            }
    }

    public void UpdateVisuals()
    {
        pnlPlayerTwo.alpha = 1f;
        txtPlayerTwo.SetActive(true);
        txtWaiting.SetActive(false);
        hasUpdatedVisuals = true;
    }

    public void ChangeVisualsBack()
    {
        pnlPlayerTwo.alpha = 0.5f;
        txtPlayerTwo.SetActive(false);
        txtWaiting.SetActive(true);
        hasUpdatedVisuals = false;
    }

    public async void LeaveLobby()
    {
        if (currentLobby == null) return;

        // STOP ALL BACKGROUND PROCESSES FIRST
        shouldStopUpdateLoop = true;
        isUpdatingLobby = false;

        // Stop heartbeat if we're the host
        if (isHost && LobbyManager.Instance != null)
        {
            LobbyManager.Instance.StopAllLobbyProcesses();
        }

        if (isHost)
        {
            // Remove all other players first
            foreach (var player in currentLobby.Players)
            {
                if (player.Id != AuthenticationService.Instance.PlayerId)
                {
                    try
                    {
                        await LobbyService.Instance.RemovePlayerAsync(
                            currentLobby.Id,
                            player.Id
                        );
                    }
                    catch (LobbyServiceException e)
                    {
                        Debug.LogWarning($"Failed to remove player {player.Id}: {e.Message}");
                    }
                }
            }

            // Delete the lobby
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                Debug.Log("Lobby deleted successfully");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to delete lobby: {e.Message}");
            }
        }
        else
        {
            // Client just removes themselves
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    currentLobby.Id,
                    AuthenticationService.Instance.PlayerId
                );
                Debug.Log("Player removed from lobby");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to remove player: {e.Message}");
            }
        }

        // Clean up
        currentLobby = null;

        // Switch UI panels
        lobbyRoomPanel.SetActive(false);
        lobbyListParent.SetActive(true);

        // Reset flags for next lobby session
        shouldStopUpdateLoop = false;
        hasUpdatedVisuals = false;
        isReady = false;

        // Refresh the lobby list
        if (LobbyManager.Instance != null)
        {
            // Reset lobby manager flags
            LobbyManager.Instance.ResetForNewLobby();
        }
    }

    private void OnDestroy()
    {
        // Clean up references when this object is destroyed
        StopAllBackgroundProcesses();
        currentLobby = null;
        isUpdatingLobby = false;
    }

    private void OnDisable()
    {
        if (currentLobby != null)
        {
            shouldStopUpdateLoop = true;
            isUpdatingLobby = false;
        }
    }*/


    public static LobbyRoomUI Instance;

    [SerializeField] private GameObject lobbyRoomPanel;
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    public AudioClip radioStaticSong;
    public AudioClip ambientSong;

    [SerializeField] private Button btnReadyOne;
    [SerializeField] private Button btnReadyTwo;
    [SerializeField] private TextMeshProUGUI txtReadyOne;
    [SerializeField] private TextMeshProUGUI txtReadyTwo;
    [SerializeField] private GameObject imgDeckOne;
    [SerializeField] private GameObject imgDeckTwo;
    [SerializeField] private GameObject imgCabinOne;
    [SerializeField] private GameObject imgCabinTwo;

    [SerializeField] CanvasGroup pnlPlayerTwo;

    [SerializeField] private TextMeshProUGUI txtLobbyName;
    [SerializeField] private Button btnLeave;

    [SerializeField] private bool isReady = false;
    [SerializeField] private bool isHost;
    [SerializeField] private GameObject txtPlayerTwo;
    [SerializeField] private GameObject txtWaiting;

    [SerializeField] private GameObject lobbyListParent;

    private Lobby currentLobby;

    private bool isUpdatingLobby = false;
    private bool hasUpdatedVisuals = false;
    private bool shouldStopUpdateLoop = false;

    // Scene transition
    private bool isTransitioning = false;
    private float fakeProgress = 0f;

    private void Awake()
    {
        Instance = this;

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private void OnSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        Debug.Log($"Scene load started for client {clientId}, scene: {sceneName}");

        // Show loading screen for ALL clients when scene starts loading
        ShowLoadingScreen("Loading game...");
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Scene load complete for client {clientId}, scene: {sceneName}");

        // Hide loading screen (optional - it will be destroyed with the scene anyway)
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    public void OpenLobbyRoom(Lobby lobby, bool isDeck)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            // Remove first to avoid duplicate subscriptions
            NetworkManager.Singleton.SceneManager.OnLoad -= OnSceneLoad;
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;

            // Add fresh subscriptions
            NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;

            Debug.Log("Subscribed to scene load events");
        }
        else
        {
            Debug.LogError("NetworkManager or SceneManager is null! Is the NetworkManager properly initialized?");
        }

        currentLobby = lobby;
        lobbyListParent.SetActive(false);
        lobbyRoomPanel.SetActive(true);

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(radioStaticSong, "RadioStatic");

        string myID = AuthenticationService.Instance.PlayerId;
        isHost = currentLobby.HostId == myID;

        txtLobbyName.text = currentLobby.Name;

        // Default UI state
        txtReadyOne.text = "Not Ready";
        txtReadyTwo.text = "Not Ready";

        btnReadyOne.GetComponent<Image>().color = Color.red;
        btnReadyTwo.GetComponent<Image>().color = Color.red;

        imgDeckOne.SetActive(false);
        imgDeckTwo.SetActive(false);
        imgCabinOne.SetActive(false);
        imgCabinTwo.SetActive(false);

        if (isHost)
        {
            pnlPlayerTwo.alpha = 0.5f;
            txtPlayerTwo.SetActive(false);
            txtWaiting.SetActive(true);
            if (isDeck)
            {
                SessionData.Instance.isHostDeck = true;
                imgDeckOne.SetActive(true);
                imgCabinTwo.SetActive(true);
            }
            else
            {
                SessionData.Instance.isHostDeck = false;
                imgCabinOne.SetActive(true);
                imgDeckTwo.SetActive(true);
            }
        }
        else
        {
            if (isDeck)
            {
                SessionData.Instance.isHostDeck = false;
                imgDeckTwo.SetActive(true);
                imgCabinOne.SetActive(true);
            }
            else
            {
                SessionData.Instance.isHostDeck = true;
                imgCabinTwo.SetActive(true);
                imgDeckOne.SetActive(true);
            }
        }

        RefreshUI();

        if (!isUpdatingLobby)
        {
            UpdateLobbyLoop();
        }
    }

    private void ShowLoadingScreen(string message)
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (loadingBar != null)
                loadingBar.value = 0f;

            // Start fake progress animation
            StartCoroutine(AnimateLoadingBar());
        }
    }

    private IEnumerator AnimateLoadingBar()
    {
        fakeProgress = 0f;

        while (loadingScreen != null && loadingScreen.activeInHierarchy)
        {
            // Slowly fill the bar to give visual feedback
            fakeProgress += Time.deltaTime * 0.3f; // Takes about 3-4 seconds to fill
            fakeProgress = Mathf.Min(fakeProgress, 0.9f); // Cap at 90% until actually loaded

            if (loadingBar != null)
                loadingBar.value = fakeProgress;

            yield return null;
        }
    }

    public async void ToggleReady()
    {
        if (currentLobby == null || isTransitioning)
        {
            Debug.LogError("Current lobby is null or already transitioning!");
            return;
        }

        isReady = !isReady;
        string newState = isReady ? "true" : "false";

        Debug.Log($"Toggling ready state to: {isReady} for player: {AuthenticationService.Instance.PlayerId}");

        var data = new Dictionary<string, PlayerDataObject>
        {
            {
                "Ready",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newState)
            }
        };
        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions { Data = data }
            );

            await Task.Delay(500);
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

            Debug.Log($"Lobby refreshed. Players: {currentLobby.Players.Count}");

            if (AllPlayersReady() && isHost && !isTransitioning)
            {
                Debug.Log("All players are ready! Starting game...");
                isTransitioning = true;
                StartGameScene();
            }

            RefreshUI();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to update ready state: " + e.Message);
        }
    }

    private bool AllPlayersReady()
    {
        if (currentLobby == null || currentLobby.Players == null)
            return false;

        if (currentLobby.Players.Count < 2)
            return false;

        foreach (Unity.Services.Lobbies.Models.Player player in currentLobby.Players)
        {
            if (player.Data == null ||
                !player.Data.ContainsKey("Ready") ||
                player.Data["Ready"].Value != "true")
            {
                return false;
            }
        }

        return true;
    }

    private void StartGameScene()
    {
        string sceneName = "GameScene";

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null! Cannot load scene.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Only the host can load scenes!");
            return;
        }

        if (!CanLoad(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in the build settings!");
            return;
        }

        Debug.Log($"Host starting game scene transition...");

        // Show loading screen on the host immediately
        ShowLoadingScreen("Starting game...");

        StopAllBackgroundProcesses();

        if (NetworkManager.Singleton.SceneManager != null)
        {
            // This triggers OnSceneLoad on ALL clients automatically
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            Debug.Log("Scene loading initiated via NetworkManager");
        }
        else
        {
            Debug.LogError("SceneManager is null! Falling back to regular scene load.");
            SceneManager.LoadScene(sceneName);
        }

        // Change music
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "AmbientEnvironment");
        }
    }

    private bool CanLoad(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    private void StopAllBackgroundProcesses()
    {
        Debug.Log("Stopping all background processes...");
        shouldStopUpdateLoop = true;
        isUpdatingLobby = false;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.StopAllLobbyProcesses();
        }
    }

    private async void UpdateLobbyLoop()
    {
        if (isUpdatingLobby) return;
        isUpdatingLobby = true;

        while (!shouldStopUpdateLoop && lobbyRoomPanel != null && lobbyRoomPanel.activeInHierarchy)
        {
            await Task.Delay(1000);

            if (shouldStopUpdateLoop)
                break;

            if (this == null)
                break;


            if (currentLobby == null) 
                break;

            if (string.IsNullOrWhiteSpace(currentLobby.Id))
                break;

            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                if (currentLobby == null)
                {
                    Debug.Log("Lobby is null, game might be starting...");
                    break;
                }

                // Check if all players ready and host hasn't started transition yet
                if (isHost && AllPlayersReady() && !isTransitioning)
                {
                    Debug.Log("UpdateLobbyLoop: All players ready, starting game...");
                    isTransitioning = true;
                    StartGameScene();
                    break;
                }

                RefreshUI();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Lobby refresh failed: {e.Message}");
                break;
            }
        }

        isUpdatingLobby = false;
    }

    private void RefreshUI()
    {
        if (this == null || btnReadyOne == null || btnReadyTwo == null)
            return;

        if (currentLobby == null) return;

        bool hasTwoPlayers = currentLobby.Players.Count >= 2;

        btnReadyOne.interactable = false;
        btnReadyTwo.interactable = false;

        if (hasTwoPlayers)
        {
            if (isHost)
            {
                btnReadyOne.interactable = true;
                if (!hasUpdatedVisuals)
                {
                    UpdateVisuals();
                }
            }
            else
            {
                btnReadyTwo.interactable = true;
            }
        }
        else
        {
            ChangeVisualsBack();
        }

        for (int i = 0; i < currentLobby.Players.Count; i++)
        {
            if (i >= 2) break;

            Unity.Services.Lobbies.Models.Player player = currentLobby.Players[i];

            bool ready =
                player.Data != null &&
                player.Data.ContainsKey("Ready") &&
                player.Data["Ready"].Value == "true";

            if (i == 0)
            {
                txtReadyOne.text = ready ? "Ready!" : "Not Ready";
                btnReadyOne.GetComponent<Image>().color =
                    ready ? Color.green : Color.red;
            }
            else if (i == 1)
            {
                txtReadyTwo.text = ready ? "Ready!" : "Not Ready";
                btnReadyTwo.GetComponent<Image>().color =
                    ready ? Color.green : Color.red;
            }
        }
    }

    public void UpdateVisuals()
    {
        pnlPlayerTwo.alpha = 1f;
        txtPlayerTwo.SetActive(true);
        txtWaiting.SetActive(false);
        hasUpdatedVisuals = true;
    }

    public void ChangeVisualsBack()
    {
        pnlPlayerTwo.alpha = 0.5f;
        txtPlayerTwo.SetActive(false);
        txtWaiting.SetActive(true);
        hasUpdatedVisuals = false;
    }

    public async void LeaveLobby()
    {
        if (currentLobby == null) return;

        shouldStopUpdateLoop = true;
        isUpdatingLobby = false;
        isTransitioning = false;

        if (isHost && LobbyManager.Instance != null)
        {
            LobbyManager.Instance.StopAllLobbyProcesses();
        }

        if (isHost)
        {
            foreach (var player in currentLobby.Players)
            {
                if (player.Id != AuthenticationService.Instance.PlayerId)
                {
                    try
                    {
                        await LobbyService.Instance.RemovePlayerAsync(
                            currentLobby.Id,
                            player.Id
                        );
                    }
                    catch (LobbyServiceException e)
                    {
                        Debug.LogWarning($"Failed to remove player {player.Id}: {e.Message}");
                    }
                }
            }

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                Debug.Log("Lobby deleted successfully");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to delete lobby: {e.Message}");
            }
        }
        else
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    currentLobby.Id,
                    AuthenticationService.Instance.PlayerId
                );
                Debug.Log("Player removed from lobby");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to remove player: {e.Message}");
            }
        }

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "SeaAmbience");

        currentLobby = null;
        lobbyRoomPanel.SetActive(false);
        lobbyListParent.SetActive(true);

        shouldStopUpdateLoop = false;
        hasUpdatedVisuals = false;
        isReady = false;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ResetForNewLobby();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoad -= OnSceneLoad;
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        }

        StopAllBackgroundProcesses();
        currentLobby = null;
        isUpdatingLobby = false;
        Instance = null;
    }

    private void OnDisable()
    {
        if (currentLobby != null)
        {
            shouldStopUpdateLoop = true;
            isUpdatingLobby = false;
        }
    }
}

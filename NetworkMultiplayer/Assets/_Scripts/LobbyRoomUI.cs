// ─── LobbyRoomUI.cs ──────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyRoomUI : MonoBehaviour
{
    public static LobbyRoomUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject lobbyRoomPanel;
    [SerializeField] private GameObject lobbyListParent;

    [Header("Player One (Host)")]
    [SerializeField] private Button btnReadyOne;
    [SerializeField] private TextMeshProUGUI txtReadyOne;
    [SerializeField] private GameObject imgDeckOne;
    [SerializeField] private GameObject imgCabinOne;

    [Header("Player Two (Client)")]
    [SerializeField] private Button btnReadyTwo;
    [SerializeField] private TextMeshProUGUI txtReadyTwo;
    [SerializeField] private GameObject imgDeckTwo;
    [SerializeField] private GameObject imgCabinTwo;
    [SerializeField] private CanvasGroup pnlPlayerTwo;
    [SerializeField] private GameObject txtPlayerTwo;
    [SerializeField] private GameObject txtWaiting;

    [Header("Misc")]
    [SerializeField] private TextMeshProUGUI txtLobbyName;

    [Header("Audio")]
    [SerializeField] private AudioClip radioStaticSong;
    [SerializeField] private AudioClip ambientSong;

    private Lobby _currentLobby;
    private bool _isHost;
    private bool _isTransitioning;
    private bool _hasUpdatedVisuals;

    private const string GameSceneName = "GameScene";

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        _currentLobby = null;
        Instance = null;
    }

    // ─── Open ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the lobby room. isDeck = true means THIS player is the deck role.
    /// Host: called immediately after StartHost.
    /// Client: called only after NGO connection is confirmed (OnClientConnectedCallback).
    /// </summary>
    public void OpenLobbyRoom(Lobby lobby, bool isDeck)
    {
        _currentLobby = lobby;
        _isHost = AuthenticationService.Instance.PlayerId == lobby.HostId;
        _isTransitioning = false;
        _hasUpdatedVisuals = false;

        lobbyListParent.SetActive(false);
        lobbyRoomPanel.SetActive(true);

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(radioStaticSong, "RadioStatic");

        txtLobbyName.text = lobby.Name;

        // Reset ready buttons
        //ApplyReadyState(btnReadyOne, txtReadyOne, false);
        //ApplyReadyState(btnReadyTwo, txtReadyTwo, false);
        ResetReadyUI();
        btnReadyOne.interactable = false;
        btnReadyTwo.interactable = false;

        // Role icons: slot 1 = host, slot 2 = client
        imgDeckOne.SetActive(false); imgCabinOne.SetActive(false);
        imgDeckTwo.SetActive(false); imgCabinTwo.SetActive(false);

        if (_isHost)
        {
            // isDeck = is host the deck player
            imgDeckOne.SetActive(isDeck); imgCabinOne.SetActive(!isDeck);
            imgCabinTwo.SetActive(isDeck); imgDeckTwo.SetActive(!isDeck);

            // Show waiting state for slot 2 until client joins
            pnlPlayerTwo.alpha = 0.5f;
            txtPlayerTwo.SetActive(false);
            txtWaiting.SetActive(true);
        }
        else
        {
            // isDeck = is THIS client the deck player
            imgDeckTwo.SetActive(isDeck); imgCabinTwo.SetActive(!isDeck);
            imgDeckOne.SetActive(!isDeck); imgCabinOne.SetActive(isDeck);
        }

        RefreshUI();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        //if (!_isUpdatingLobby) _ = UpdateLobbyLoop();
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[LobbyRoomUI] Client connected: {clientId}");

        RefreshUI();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[LobbyRoomUI] Client disconnected: {clientId}");

        if (_isTransitioning)
            return;

        // HOST SIDE:
        // Client left lobby
        if (NetworkManager.Singleton.IsHost)
        {
            ReadyManager.Instance?.ResetReadyStates();

            ResetReadyUI();

            RefreshUI();

            Debug.Log("[LobbyRoomUI] Client left. Ready states reset.");
        }
        else
        {
            // CLIENT SIDE:
            // Host disappeared
            LeaveLobby();
        }
    }

    // ─── Ready ───────────────────────────────────────────────────────────────

    public void ToggleReady()
    {
        if (_isTransitioning) return;

        if (ReadyManager.Instance == null)
        {
            Debug.LogError("ReadyManager instance missing!");
            return;
        }

        ReadyManager.Instance.ToggleReady();
    }

    public void RefreshReadyVisuals(bool hostReady, bool clientReady)
    {
        ApplyReadyState(btnReadyOne, txtReadyOne, hostReady);
        ApplyReadyState(btnReadyTwo, txtReadyTwo, clientReady);
    }

    // ─── Game start ──────────────────────────────────────────────────────────

    private void StartGameTransition()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("[NGO] Only the host may trigger a scene transition.");
            return;
        }

        if (!IsSceneInBuildSettings(GameSceneName))
        {
            Debug.LogError($"[NGO] '{GameSceneName}' is not in build settings!");
            return;
        }

        StopAllBackgroundProcesses();

        Debug.Log("[NGO] Loading game scene for all clients…");
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "AmbientEnvironment");
    }

    // ─── Leave lobby ─────────────────────────────────────────────────────────

    /*public async void LeaveLobby()
    {
        if (_currentLobby == null) return;

        StopAllBackgroundProcesses();

        string lobbyId = _currentLobby.Id;
        string myId = AuthenticationService.Instance.PlayerId;

        try
        {
            if (_isHost)
            {
                foreach (var player in _currentLobby.Players)
                {
                    if (player.Id == myId) continue;
                    try { await LobbyService.Instance.RemovePlayerAsync(lobbyId, player.Id); }
                    catch (LobbyServiceException e) { Debug.LogWarning($"[LobbyRoom] Kick failed: {e.Message}"); }
                }
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log("[LobbyRoom] Lobby deleted.");
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, myId);
                Debug.Log("[LobbyRoom] Left lobby.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyRoom] Leave error: {e.Message}");
        }

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(ambientSong, "SeaAmbience");

        _currentLobby = null;
        _isReady = false;
        _isTransitioning = false;
        _hasUpdatedVisuals = false;
        _stopUpdateLoop = false;

        lobbyRoomPanel.SetActive(false);
        lobbyListParent.SetActive(true);

        LobbyManager.Instance?.ResetForNewLobby();
    }*/

    public async void LeaveLobby()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        if (ReadyManager.Instance != null &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsHost)
        {
            ReadyManager.Instance.ResetReadyStates();
        }

        await LobbyManager.Instance.ShutdownNetworkAndLobby();

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.CrossfadeToNewSong(
                ambientSong,
                "SeaAmbience");
        }

        lobbyRoomPanel.SetActive(false);
        lobbyListParent.SetActive(true);

        LobbyManager.Instance.ResetForNewLobby();

        _currentLobby = null;

        _hasUpdatedVisuals = false;
        _isTransitioning = false;

        Debug.Log("[LobbyRoom] Returned to lobby browser.");
    }

    // ─── UI helpers ──────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_currentLobby == null || btnReadyOne == null) return;

        //bool hasTwoPlayers = _currentLobby.Players.Count >= 2;

        bool hasTwoPlayers =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening &&
            NetworkManager.Singleton.ConnectedClientsList.Count >= 2;

        //btnReadyOne.interactable = _isHost && hasTwoPlayers;
        //btnReadyTwo.interactable = !_isHost && hasTwoPlayers;

        bool locked = ReadyManager.Instance != null &&
              ReadyManager.Instance.IsGameStarting;

        btnReadyOne.interactable =
            _isHost &&
            hasTwoPlayers &&
            !locked;

        btnReadyTwo.interactable =
            !_isHost &&
            hasTwoPlayers &&
            !locked;

        if (_isHost)
        {
            if (hasTwoPlayers && !_hasUpdatedVisuals)
            {
                pnlPlayerTwo.alpha = 1f;
                txtPlayerTwo.SetActive(true);
                txtWaiting.SetActive(false);
                _hasUpdatedVisuals = true;
            }
            else if (!hasTwoPlayers && _hasUpdatedVisuals)
            {
                pnlPlayerTwo.alpha = 0.5f;
                txtPlayerTwo.SetActive(false);
                txtWaiting.SetActive(true);
                _hasUpdatedVisuals = false;
            }
        }
    }

    private static void ApplyReadyState(Button btn, TextMeshProUGUI label, bool ready)
    {
        label.text = ready ? "Ready!" : "Not Ready";
        btn.GetComponent<Image>().color = ready ? Color.green : Color.red;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void StopAllBackgroundProcesses()
    {
        LobbyManager.Instance?.StopAllLobbyProcesses();
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return true;
        }
        return false;
    }

    private void ResetReadyUI()
    {
        ApplyReadyState(btnReadyOne, txtReadyOne, false);
        ApplyReadyState(btnReadyTwo, txtReadyTwo, false);

        btnReadyOne.interactable = false;
        btnReadyTwo.interactable = false;
    }

    public void LockLobbyUI()
    {
        btnReadyOne.interactable = false;
        btnReadyTwo.interactable = false;

        _isTransitioning = true;
    }
}
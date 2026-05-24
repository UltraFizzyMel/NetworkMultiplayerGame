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
    private bool _isReady;
    private bool _isTransitioning;
    private bool _hasUpdatedVisuals;
    private bool _isUpdatingLobby;
    private bool _stopUpdateLoop;

    private const string GameSceneName = "GameScene";

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        _stopUpdateLoop = true;
        _isUpdatingLobby = false;
        _currentLobby = null;
        Instance = null;
    }

    private void OnDisable() => _stopUpdateLoop = true;

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
        _isReady = false;
        _isTransitioning = false;
        _stopUpdateLoop = false;
        _hasUpdatedVisuals = false;

        lobbyListParent.SetActive(false);
        lobbyRoomPanel.SetActive(true);

        if (MusicManager.Instance != null)
            MusicManager.Instance.CrossfadeToNewSong(radioStaticSong, "RadioStatic");

        txtLobbyName.text = lobby.Name;

        // Reset ready buttons
        ApplyReadyState(btnReadyOne, txtReadyOne, false);
        ApplyReadyState(btnReadyTwo, txtReadyTwo, false);
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

        if (!_isUpdatingLobby) _ = UpdateLobbyLoop();
    }

    // ─── Ready ───────────────────────────────────────────────────────────────

    public async void ToggleReady()
    {
        if (_currentLobby == null || _isTransitioning) return;

        _isReady = !_isReady;

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                _currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "Ready", new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            _isReady ? "true" : "false") }
                    }
                });

            _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

            if (_isHost && AllPlayersReady() && !_isTransitioning)
            {
                _isTransitioning = true;
                StartGameTransition();
                return;
            }

            RefreshUI();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyRoom] Ready toggle failed: {e.Message}");
            _isReady = !_isReady; // Revert on failure
        }
    }

    /*public async void ToggleReady()
    {
        if (_currentLobby == null || _isTransitioning) return;

        _isReady = !_isReady;

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(
                _currentLobby.Id,
                AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                    { "Ready", new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        _isReady ? "true" : "false") }
                    }
                });

            // Add a small delay to let the lobby service process the update
            await Task.Delay(300);

            // Try to get updated lobby, but don't fail if it errors
            try
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            }
            catch (System.Exception)
            {
                // Lobby service hiccup - the update succeeded, so just keep current lobby
                Debug.LogWarning("[LobbyRoom] GetLobbyAsync failed, but UpdatePlayerAsync succeeded. Continuing...");
            }

            if (_currentLobby != null)
            {
                if (_isHost && AllPlayersReady() && !_isTransitioning)
                {
                    _isTransitioning = true;
                    StartGameTransition();
                    return;
                }

                RefreshUI();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyRoom] Ready toggle failed: {e.Message}");
            _isReady = !_isReady; // Revert on failure
        }
    }*/

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

    public async void LeaveLobby()
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
    }

    // ─── Update loop ─────────────────────────────────────────────────────────

    private async Task UpdateLobbyLoop()
    {
        if (_isUpdatingLobby) return;
        _isUpdatingLobby = true;

        while (!_stopUpdateLoop
               && _currentLobby != null
               && lobbyRoomPanel != null
               && lobbyRoomPanel.activeInHierarchy)
        {
            await Task.Delay(2000);

            if (_stopUpdateLoop || _currentLobby == null) break;

            try
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);

                if (_currentLobby == null) break;

                if (_isHost && AllPlayersReady() && !_isTransitioning)
                {
                    _isTransitioning = true;
                    StartGameTransition();
                    break;
                }

                RefreshUI();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LobbyRoom] Poll failed: {e.Message}");
                break;
            }
        }

        _isUpdatingLobby = false;
    }

    // ─── UI helpers ──────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_currentLobby == null || btnReadyOne == null) return;

        bool hasTwoPlayers = _currentLobby.Players.Count >= 2;

        btnReadyOne.interactable = _isHost && hasTwoPlayers;
        btnReadyTwo.interactable = !_isHost && hasTwoPlayers;

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

        for (int i = 0; i < Mathf.Min(_currentLobby.Players.Count, 2); i++)
        {
            var player = _currentLobby.Players[i];
            bool ready = player.Data != null
                       && player.Data.ContainsKey("Ready")
                       && player.Data["Ready"].Value == "true";

            if (i == 0) ApplyReadyState(btnReadyOne, txtReadyOne, ready);
            else ApplyReadyState(btnReadyTwo, txtReadyTwo, ready);
        }
    }

    private static void ApplyReadyState(Button btn, TextMeshProUGUI label, bool ready)
    {
        label.text = ready ? "Ready!" : "Not Ready";
        btn.GetComponent<Image>().color = ready ? Color.green : Color.red;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool AllPlayersReady()
    {
        if (_currentLobby?.Players == null || _currentLobby.Players.Count < 2) return false;

        foreach (var player in _currentLobby.Players)
        {
            if (player.Data == null
                || !player.Data.ContainsKey("Ready")
                || player.Data["Ready"].Value != "true") return false;
        }
        return true;
    }

    private void StopAllBackgroundProcesses()
    {
        _stopUpdateLoop = true;
        _isUpdatingLobby = false;
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
}
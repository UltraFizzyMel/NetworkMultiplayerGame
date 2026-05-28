// ─── LobbyManager.cs ─────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby Creation")]
    [SerializeField] private GameObject lobbyCreationParent;
    [SerializeField] private Button btnCreateLobby;
    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyPasswordField;
    [SerializeField] private Toggle isPrivate;

    [Header("Lobby List")]
    [SerializeField] private GameObject lobbyListParent;
    [SerializeField] private Transform lobbyContentParent;
    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private TMP_InputField lobbySearchField;

    [Header("Password Panel")]
    [SerializeField] private GameObject pnlPassword;
    [SerializeField] private TMP_InputField txtPassword;
    [SerializeField] private GameObject pnlFailedToJoin;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnSubmit;
    [SerializeField] private Button btnBack;

    public Lobby CurrentLobby { get; private set; }
    public string JoinedLobbyID { get; private set; }

    // Cached while we wait for the NGO connection to establish
    private Lobby _pendingLobby;
    private bool _pendingClientIsDeck;
    private string _selectedLobbyID;

    private bool _isCreatingLobby;
    private bool _isJoiningLobby;
    private bool _isRefreshingList;
    private bool _stopHeartbeat;
    private bool _stopLobbyList;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private async void Start()
    {
        Instance = this;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn &&
            !AuthenticationService.Instance.IsAuthorized)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        TogglePasswordField();
        ExitLobbyCreationPanel(); // Shows the lobby list on startup
    }

    private void OnDestroy()
    {
        StopAllLobbyProcesses();
        UnsubscribeNGOCallbacks();
        Instance = null;
    }

    private async void OnApplicationQuit()
    {
        if (CurrentLobby == null || string.IsNullOrWhiteSpace(CurrentLobby.Id)) return;
        try { await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id); } catch { }
    }

    // ─── Lobby Creation ──────────────────────────────────────────────────────

    public void OpenLobbyCreationPanel()
    {
        _stopLobbyList = true;
        lobbyListParent.SetActive(false);
        lobbyCreationParent.SetActive(true);
    }

    public void ExitLobbyCreationPanel()
    {
        lobbyCreationParent.SetActive(false);
        lobbyListParent.SetActive(true);
        TryStartLobbyList();
    }

    public void TogglePasswordField()
    {
        createLobbyPasswordField.interactable = isPrivate.isOn;
        if (!isPrivate.isOn) createLobbyPasswordField.text = string.Empty;
    }

    public async void CreateLobby()
    {
        if (_isCreatingLobby) return;
        _isCreatingLobby = true;
        btnCreateLobby.interactable = false;

        string lobbyName = string.IsNullOrWhiteSpace(createLobbyNameField.text)
            ? "Game Lobby" : createLobbyNameField.text;

        bool privateLobby = isPrivate.isOn;
        string hostRole = Random.Range(0, 2) == 0 ? "Deck" : "Cabin";

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Lobby] Host relay join code: {joinCode}");

            var lobbyData = new Dictionary<string, DataObject>
            {
                { "HostRole", new DataObject(DataObject.VisibilityOptions.Public, hostRole) },
                { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            };

            if (privateLobby)
            {
                string pw = string.IsNullOrWhiteSpace(createLobbyPasswordField.text)
                    ? "1234" : createLobbyPasswordField.text;
                lobbyData["HasPassword"] = new DataObject(DataObject.VisibilityOptions.Public, "true");
                lobbyData["Password"] = new DataObject(DataObject.VisibilityOptions.Public, pw);
            }

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName, 2, new CreateLobbyOptions { IsPrivate = false, Data = lobbyData });

            if (lobby == null) { Debug.LogError("[Lobby] CreateLobbyAsync returned null."); return; }

            CurrentLobby = lobby;
            JoinedLobbyID = lobby.Id;
            Debug.Log($"[Lobby] Created '{lobby.Name}' (ID: {lobby.Id})");

            // Configure relay transport for host
            UnityTransport transport = GetTransport();
            if (transport == null) return;

            var endpoint = GetEndpointForAllocation(
                allocation.ServerEndpoints, allocation.RelayServer.IpV4,
                allocation.RelayServer.Port, out bool isSecure);

            transport.SetHostRelayData(
                endpoint.Address.Split(':')[0], endpoint.Port,
                allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, isSecure);

            NetworkManager.Singleton.StartHost();
            Debug.Log("[NGO] Host started.");

            bool hostIsDeck = hostRole == "Deck";
            SessionData.Instance.isHostDeck = hostIsDeck;

            lobbyCreationParent.SetActive(false);
            lobbyListParent.SetActive(true);

            LobbyRoomUI.Instance.OpenLobbyRoom(lobby, hostIsDeck);
            _ = HeartbeatLoop(lobby);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] CreateLobby error: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            _isCreatingLobby = false;
            btnCreateLobby.interactable = true;
        }
    }

    // ─── Joining ─────────────────────────────────────────────────────────────

    public void JoinLobby(string lobbyID, bool needPassword)
    {
        if (_isJoiningLobby) return;

        _selectedLobbyID = lobbyID;

        if (needPassword)
        {
            SetJoinControlsInteractable(false);
            pnlPassword.SetActive(true);
        }
        else
        {
            _ = JoinLobbyAsync();
        }
    }

    private async Task JoinLobbyAsync(string prefetchedJoinCode = null)
    {
        if (_isJoiningLobby) return;
        _isJoiningLobby = true;

        try
        {
            if (string.IsNullOrWhiteSpace(_selectedLobbyID)) return;

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(_selectedLobbyID);
            CurrentLobby = lobby;
            JoinedLobbyID = lobby.Id;
            Debug.Log($"[Lobby] Joined '{lobby.Name}'");

            // Resolve join code ------------------------------------------------
            string joinCode = prefetchedJoinCode
                ?? (lobby.Data != null && lobby.Data.ContainsKey("JoinCode")
                    ? lobby.Data["JoinCode"].Value : null);

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                await Task.Delay(1500);
                for (int i = 0; i < 5 && string.IsNullOrWhiteSpace(joinCode); i++)
                {
                    await Task.Delay(1000);
                    try
                    {
                        lobby = await LobbyService.Instance.GetLobbyAsync(JoinedLobbyID);
                        if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
                            joinCode = lobby.Data["JoinCode"].Value;
                    }
                    catch (LobbyServiceException e) when (i < 4)
                    {
                        Debug.LogWarning($"[Lobby] JoinCode retry {i + 1}: {e.Message}");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("[Lobby] Could not obtain a relay join code.");
                await TryLeaveCurrentLobby();
                return;
            }

            // Join relay allocation -------------------------------------------
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("[Relay] Allocation joined.");

            UnityTransport transport = GetTransport();
            if (transport == null) { await TryLeaveCurrentLobby(); return; }

            var endpoint = GetEndpointForAllocation(
                joinAlloc.ServerEndpoints, joinAlloc.RelayServer.IpV4,
                joinAlloc.RelayServer.Port, out bool isSecure);

            transport.SetClientRelayData(
                endpoint.Address.Split(':')[0], endpoint.Port,
                joinAlloc.AllocationIdBytes, joinAlloc.Key,
                joinAlloc.ConnectionData, joinAlloc.HostConnectionData, isSecure);

            // Cache lobby data — OpenLobbyRoom is called only once NGO confirms
            // the connection (see HandleClientConnected below).
            string hostRole = lobby.Data != null && lobby.Data.ContainsKey("HostRole")
                                      ? lobby.Data["HostRole"].Value : "Deck";
            _pendingLobby = lobby;
            _pendingClientIsDeck = hostRole != "Deck";
            SessionData.Instance.isHostDeck = hostRole == "Deck";

            // Subscribe BEFORE StartClient so we never miss the callback
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            NetworkManager.Singleton.StartClient();
            Debug.Log("[NGO] StartClient called – awaiting connection…");

            // Safety timeout (10 s) in case the callbacks never fire
            _ = ConnectionTimeoutAsync(10f);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Join failed: {e.Message}");
            await TryLeaveCurrentLobby();
            ResetJoinState();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Join failed: {e.Message}");
            ResetJoinState();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Lobby] Unexpected join error: {e.Message}");
            ResetJoinState();
        }
        // Note: _isJoiningLobby is intentionally NOT reset here.
        // It's reset in the connection callback (or the timeout).
    }

    // Called by NGO on the client when it successfully connects to the host.
    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer) return; // Guard; host shouldn't reach here

        UnsubscribeNGOCallbacks();
        Debug.Log($"[NGO] Client connected (ID: {clientId})");

        ResetJoinState();

        if (_pendingLobby != null)
            LobbyRoomUI.Instance.OpenLobbyRoom(_pendingLobby, _pendingClientIsDeck);
    }

    // Called if the client gets disconnected before or after connecting.
    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer) return;

        UnsubscribeNGOCallbacks();
        Debug.LogError("[NGO] Connection to host lost or failed.");
        ResetJoinState();
        _pendingLobby = null;
    }

    private async Task ConnectionTimeoutAsync(float seconds)
    {
        await Task.Delay((int)(seconds * 1000));
        if (!_isJoiningLobby) return; // Already resolved

        Debug.LogError("[NGO] Connection timed out.");
        UnsubscribeNGOCallbacks();
        NetworkManager.Singleton.Shutdown();
        await TryLeaveCurrentLobby();
        ResetJoinState();
        _pendingLobby = null;
    }

    private void UnsubscribeNGOCallbacks()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void ResetJoinState()
    {
        _isJoiningLobby = false;
        SetJoinControlsInteractable(true);
        if (pnlPassword != null) pnlPassword.SetActive(false);
    }

    // ─── Password panel ──────────────────────────────────────────────────────

    public async void SubmitPassword()
    {
        if (string.IsNullOrWhiteSpace(_selectedLobbyID)) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(_selectedLobbyID);

            if (!lobby.Data.ContainsKey("Password"))
            {
                Debug.LogError("[Lobby] No Password field in lobby data.");
                return;
            }

            if (txtPassword.text != lobby.Data["Password"].Value)
            {
                pnlPassword.SetActive(false);
                pnlFailedToJoin.SetActive(true);
                return;
            }

            string joinCode = lobby.Data.ContainsKey("JoinCode") ? lobby.Data["JoinCode"].Value : null;
            pnlPassword.SetActive(false);
            txtPassword.text = string.Empty;

            await JoinLobbyAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Lobby] Password check failed: {e.Message}");
            if (e.Reason == LobbyExceptionReason.LobbyNotFound) ClosePasswordPanel();
        }
    }

    public void ClosePasswordPanel()
    {
        pnlPassword.SetActive(false);
        txtPassword.text = string.Empty;
        SetJoinControlsInteractable(true);
    }

    public void CloseFailedToJoinPanel()
    {
        pnlFailedToJoin.SetActive(false);
        pnlPassword.SetActive(true);
        btnClose.interactable = true;
        btnSubmit.interactable = true;
    }

    // ─── Lobby list ──────────────────────────────────────────────────────────

    private void TryStartLobbyList()
    {
        _stopLobbyList = false;
        if (!_isRefreshingList) _ = LobbyListLoop();
    }

    private async Task LobbyListLoop()
    {
        if (_isRefreshingList) return;
        _isRefreshingList = true;

        while (Application.isPlaying && !_stopLobbyList
               && lobbyListParent != null && lobbyListParent.activeInHierarchy)
        {
            try
            {
                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

                foreach (Transform child in lobbyContentParent) Destroy(child.gameObject);

                foreach (Lobby lobby in response.Results)
                {
                    Transform item = Instantiate(lobbyItemPrefab, lobbyContentParent, false);

                    JoinLobbyButton btn = item.GetComponent<JoinLobbyButton>()
                                      ?? item.gameObject.AddComponent<JoinLobbyButton>();

                    btn.lobbyID = lobby.Id;
                    btn.needPassword = lobby.Data != null && lobby.Data.ContainsKey("HasPassword");

                    item.GetChild(0).GetComponent<TextMeshProUGUI>().text = lobby.Name;
                    item.GetChild(1).GetComponent<TextMeshProUGUI>().text = btn.needPassword ? "Private" : "Public";
                    item.GetChild(2).GetComponent<TextMeshProUGUI>().text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
                }
            }
            catch (LobbyServiceException e) { Debug.LogError($"[Lobby] Query failed: {e.Message}"); }
            catch (System.Exception e) { Debug.LogError($"[Lobby] List error: {e.Message}"); break; }

            await Task.Delay(2000);
        }

        _isRefreshingList = false;
    }

    // ─── Heartbeat ───────────────────────────────────────────────────────────

    private async Task HeartbeatLoop(Lobby lobby)
    {
        _stopHeartbeat = false;
        while (!_stopHeartbeat && Application.isPlaying && lobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
                Debug.Log($"[Lobby] Heartbeat: {lobby.Id}");
            }
            catch (LobbyServiceException e) { Debug.LogWarning($"[Lobby] Heartbeat failed: {e.Message}"); break; }

            await Task.Delay(15_000); // Unity Lobby expires after 30 s without a ping
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void StopAllLobbyProcesses()
    {
        _stopHeartbeat = true;
        _stopLobbyList = true;
        _isRefreshingList = false;
    }

    public void ResetForNewLobby()
    {
        StopAllLobbyProcesses();
        _stopHeartbeat = false;
        _stopLobbyList = false;
        _isCreatingLobby = false;
        _isJoiningLobby = false;
        CurrentLobby = null;
        JoinedLobbyID = null;
        _pendingLobby = null;
        TryStartLobbyList();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private NetworkEndpoint GetEndpointForAllocation(
        List<RelayServerEndpoint> endpoints, string ip, int port, out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS && !UNITY_WEBGL
        foreach (RelayServerEndpoint ep in endpoints)
        {
            if (ep.Secure && ep.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                isSecure = true;
                return NetworkEndpoint.Parse(ep.Host, (ushort)ep.Port);
            }
        }
#endif
        isSecure = false;
        return NetworkEndpoint.Parse(ip, (ushort)port);
    }

    private UnityTransport GetTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Lobby] NetworkManager is null!");
            return null;
        }

        UnityTransport t =
            NetworkManager.Singleton.GetComponent<UnityTransport>()
            ?? NetworkManager.Singleton.GetComponentInChildren<UnityTransport>()
            ?? NetworkManager.Singleton.NetworkConfig?.NetworkTransport as UnityTransport
            ?? FindObjectOfType<UnityTransport>();

        if (t == null) Debug.LogError("[Lobby] UnityTransport not found in scene!");
        return t;
    }

    private async Task TryLeaveCurrentLobby()
    {
        if (string.IsNullOrWhiteSpace(JoinedLobbyID)) return;
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(
                JoinedLobbyID, AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Lobby] Leave failed: {e.Message}");
        }
    }

    public async Task ShutdownNetworkAndLobby()
    {
        Debug.Log("[Lobby] Starting full shutdown...");

        UnsubscribeNGOCallbacks();
        StopAllLobbyProcesses();

        // Shutdown NGO FIRST
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();

            // Wait one frame so NGO fully cleans up
            await Task.Yield();
        }

        // Leave/delete Unity Lobby SECOND
        if (CurrentLobby != null)
        {
            try
            {
                if (AuthenticationService.Instance.PlayerId == CurrentLobby.HostId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);

                    Debug.Log("[Lobby] Deleted lobby.");
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        CurrentLobby.Id,
                        AuthenticationService.Instance.PlayerId);

                    Debug.Log("[Lobby] Left lobby.");
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[Lobby] Shutdown cleanup warning: {e.Message}");
            }
        }

        // HARD RESET ALL STATE
        CurrentLobby = null;
        JoinedLobbyID = null;

        _pendingLobby = null;

        _isCreatingLobby = false;
        _isJoiningLobby = false;
        _isRefreshingList = false;

        _stopHeartbeat = true;
        _stopLobbyList = true;

        Debug.Log("[Lobby] Shutdown complete.");
    }

    private void SetJoinControlsInteractable(bool value)
    {
        if (lobbySearchField) lobbySearchField.interactable = value;
        if (btnCreateLobby) btnCreateLobby.interactable = value;
        if (btnBack) btnBack.interactable = value;
    }
}
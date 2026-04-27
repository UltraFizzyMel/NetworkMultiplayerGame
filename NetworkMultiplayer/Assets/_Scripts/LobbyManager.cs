using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
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
    NetworkEndpoint GetEndpointForAllocation(List<RelayServerEndpoint> endpoints, string ip, int port, out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS && !UNITY_WEBGL
        // Prioritize a secure UDP endpoint if available
        foreach (RelayServerEndpoint endpoint in endpoints)
        {
            if (endpoint.Secure && endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                isSecure = true;
                return NetworkEndpoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            }
        }
#endif
        // Fallback to the default IP/port (unsecured or WebGL)
        isSecure = false;
        return NetworkEndpoint.Parse(ip, (ushort)port);
    }

    public static LobbyManager Instance { get; private set; }

    [Header("Lobby Creation")]
    [SerializeField] private GameObject lobbyCreationParent;
    [SerializeField] private Button btnCreateLobby;
    [SerializeField] private TMP_InputField createLobbyNameField;
    [SerializeField] private TMP_InputField createLobbyPasswordField;
    [SerializeField] private Toggle isPrivate;
    [SerializeField] private bool needPassword;
    [SerializeField] private string lobbyPassword;

    [Space(10)]
    [Header("Lobby List")]
    [SerializeField] private GameObject lobbyListParent;
    [SerializeField] private Transform lobbyContentParent;
    [SerializeField] private Transform lobbyItemPrefab;
    [SerializeField] private TMP_InputField lobbySearchField;
    private bool isRefreshingLobbyList = false;

    [Space(10)]
    [Header("Join Lobby")]
    [SerializeField] private GameObject pnlPassword;
    [SerializeField] private TMP_InputField txtPassword;
    [SerializeField] private GameObject pnlFailedToJoin;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnSubmit;
    [SerializeField] private Button btnBack;
    //[SerializeField] private bool correctPassword = false;
    private string selectedLobbyID;
    private string pendingJoinCode;
    private bool waitingForRelayJoin;
    private bool selectedLobbyNeedsPassword;

    //private Player playerData;
    private bool isDeck;
    //private NetworkVariable<bool> isHostDeck = new NetworkVariable<bool>();
    public Lobby currentLobby;
    public string joinedLobbyID;

    private bool shouldStopHeartbeat = false;
    private bool shouldStopLobbyList = false;

    private async void Start()
    {
        Instance = this;
        
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn &&
        !AuthenticationService.Instance.IsAuthorized)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        shouldStopHeartbeat = false;
        shouldStopLobbyList = false;

    TogglePasswordField();
        ExitLobbyCreationButton();
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void JoinLobby(string lobbyID, bool needPassword)
    {
        selectedLobbyID = lobbyID;
        selectedLobbyNeedsPassword = needPassword;

        if (needPassword)
        {
            lobbySearchField.interactable = false;
            btnCreateLobby.interactable = false;
            btnBack.interactable = false;
            pnlPassword.SetActive(true);
            return;
        }

        JoinLobbyDirectly();
    }

    /*private async void JoinLobbyDirectly(string prefetchedJoinCode = null)
    {
        try
        {
            //if (selectedLobbyID == null) return;
            if (string.IsNullOrWhiteSpace(selectedLobbyID)) return;

            try
            {
                var checkLobby = await LobbyService.Instance.GetLobbyAsync(selectedLobbyID);
            }
            catch (LobbyServiceException checkEx)
            {
                Debug.LogError($"Lobby verification failed: {checkEx.Message} (Error: {checkEx.ErrorCode})");
                Debug.LogError($"The lobby {selectedLobbyID} no longer exists!");

                // Show error and refresh lobby list
                lobbySearchField.interactable = true;
                btnCreateLobby.interactable = true;
                return;
            }

            Debug.Log($"Calling JoinLobbyByIdAsync with ID: {selectedLobbyID}");

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobbyID);

            currentLobby = lobby;
            joinedLobbyID = lobby.Id;

            Debug.Log($"Successfully joined lobby: {lobby.Name} (ID: {lobby.Id})");

            string joinCode = prefetchedJoinCode;

            // Otherwise, we need to get it from the lobby
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                if (!lobby.Data.ContainsKey("JoinCode"))
                {
                    Debug.LogError("Lobby has no JoinCode!");
                    return;
                }

                joinCode = lobby.Data["JoinCode"].Value;

                // If still empty, wait for it (but this shouldn't happen for non-password joins)
                if (string.IsNullOrWhiteSpace(joinCode))
                {
                    int maxRetries = 10;
                    int retryDelay = 500;

                    for (int i = 0; i < maxRetries; i++)
                    {
                        lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbyID);

                        Debug.Log($"Attempt {i + 1}: Checking for JoinCode...");

                        if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
                        {
                            joinCode = lobby.Data["JoinCode"].Value;
                            if (!string.IsNullOrWhiteSpace(joinCode))
                            {
                                Debug.Log($"JoinCode found after {i + 1} attempts: '{joinCode}'");
                                break;
                            }
                        }

                        if (i < maxRetries - 1)
                        {
                            Debug.Log($"JoinCode not ready yet, waiting {retryDelay}ms...");
                            await Task.Delay(retryDelay);
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("JoinCode is empty!");
                await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
                return;
            }

            Debug.Log("CLIENT JOIN CODE: " + joinCode);

            /*if (!lobby.Data.ContainsKey("JoinCode"))
            {
                Debug.LogError("Lobby has no JoinCode!");
                return;
            }*/

    //string joinCode = null;
    //int maxRetries = 10;
    //int retryDelay = 500; // milliseconds

    /*for (int i = 0; i < maxRetries; i++)
    {
        // Refresh lobby data
        lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbyID);

        Debug.Log($"Attempt {i + 1}: Checking for JoinCode...");
        Debug.Log($"Lobby Data keys: {(lobby.Data != null ? string.Join(", ", lobby.Data.Keys) : "null")}");

        if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
        {
            joinCode = lobby.Data["JoinCode"].Value;
            if (!string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.Log($"JoinCode found after {i + 1} attempts: '{joinCode}'");
                break;
            }
        }

        if (i < maxRetries - 1)
        {
            Debug.Log($"JoinCode not ready yet, waiting {retryDelay}ms...");
            await Task.Delay(retryDelay);
        }
    }

    //string joinCode =
    //lobby.Data["JoinCode"].Value;

    try
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        Debug.Log("Relay allocation joined successfully!");

        // Step 4: Configure transport
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
            return;
        }

        // Use the newer method for setting relay data
        /*transport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            true  // isSecure - set to true for dtls, false for udp
        );

        var endpoint = GetEndpointForAllocation(
            joinAllocation.ServerEndpoints,
            joinAllocation.RelayServer.IpV4,
            joinAllocation.RelayServer.Port,
            out bool isSecure);

        transport.SetClientRelayData(
            endpoint.Address.Split(':')[0], // The IP address
            endpoint.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            isSecure);

        Debug.Log("Relay data configured successfully");

        // Step 5: Start the client
        NetworkManager.Singleton.StartClient();

        // Step 6: Setup UI
        string hostRole = lobby.Data.ContainsKey("HostRole") ?
            lobby.Data["HostRole"].Value : "Deck";

        bool clientIsDeck = hostRole != "Deck";

        lobbySearchField.interactable = true;
        btnCreateLobby.interactable = true;
        pnlPassword.SetActive(false);

        LobbyRoomUI.Instance.OpenLobbyRoom(lobby, clientIsDeck);
    }
    catch (RelayServiceException relayEx)
    {
        Debug.LogError($"=== RELAY JOIN FAILED ===");
        Debug.LogError($"Message: {relayEx.Message}");
        Debug.LogError($"Error Code: {relayEx.ErrorCode}");
        Debug.LogError($"Reason: {relayEx.Reason}");
        Debug.LogError($"StackTrace: {relayEx.StackTrace}");

        // Log the join code we tried to use
        Debug.LogError($"Join Code Used: '{joinCode}'");
        Debug.LogError($"Join Code Length: {joinCode?.Length ?? 0}");

        // If relay join fails, leave the lobby
        await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);

        // Don't throw - handle gracefully
        return;
    }

    /*JoinAllocation joinAllocation =
        await RelayService.Instance.JoinAllocationAsync(joinCode);

    UnityTransport transport =
        NetworkManager.Singleton.GetComponent<UnityTransport>();

    transport.SetClientRelayData(
        joinAllocation.RelayServer.IpV4,
        (ushort)joinAllocation.RelayServer.Port,
        joinAllocation.AllocationIdBytes,
        joinAllocation.Key,
        joinAllocation.ConnectionData,
        joinAllocation.HostConnectionData,
        true
    );

    var relayServerData = new RelayServerData(joinAllocation, "dtls"); // Use "dtls" for secure or "udp" for standard
    transport.SetRelayServerData(relayServerData);

    NetworkManager.Singleton.StartClient();

    string hostRole = lobby.Data["HostRole"].Value;
    bool clientIsDeck = hostRole != "Deck";

    lobbySearchField.interactable = true;
    btnCreateLobby.interactable = true;
    pnlPassword.SetActive(false);

    LobbyRoomUI.Instance.OpenLobbyRoom(lobby, clientIsDeck);
}
catch (LobbyServiceException e)
{
    Debug.LogError("Failed to join lobby: " + e.Message);
}
}*/

    private async void JoinLobbyDirectly(string prefetchedJoinCode = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedLobbyID)) return;

            Debug.Log($"Calling JoinLobbyByIdAsync with ID: {selectedLobbyID}");

            // Join the lobby - this is the ONLY call we make initially
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobbyID);

            currentLobby = lobby;
            joinedLobbyID = lobby.Id;

            Debug.Log($"Successfully joined lobby: {lobby.Name} (ID: {lobby.Id})");

            // Check if we got a prefetched join code from password verification
            string joinCode = prefetchedJoinCode;

            // If no prefetched code, get it from the lobby data we already have
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
                {
                    joinCode = lobby.Data["JoinCode"].Value;
                }
            }

            // If still no join code (unlikely but possible), wait and retry
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogWarning("JoinCode not in initial data, waiting...");

                // Wait a bit before retrying to avoid rate limits
                await Task.Delay(1500);

                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(1000); // Wait 1 second between attempts

                    try
                    {
                        lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbyID);

                        if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
                        {
                            joinCode = lobby.Data["JoinCode"].Value;
                            if (!string.IsNullOrWhiteSpace(joinCode))
                            {
                                Debug.Log($"JoinCode found on attempt {i + 1}: '{joinCode}'");
                                break;
                            }
                        }
                    }
                    catch (LobbyServiceException e)
                    {
                        Debug.LogWarning($"Retry {i + 1} failed: {e.Message}");
                        if (i >= 4) throw; // Rethrow on last attempt
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("JoinCode is empty after all attempts!");
                await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
                return;
            }

            Debug.Log("CLIENT JOIN CODE: " + joinCode);

            // Join relay
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log("Relay allocation joined successfully!");

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                if (transport == null)
                {
                    Debug.LogError("UnityTransport component not found on NetworkManager!");
                    await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
                    return;
                }

                var endpoint = GetEndpointForAllocation(
                    joinAllocation.ServerEndpoints,
                    joinAllocation.RelayServer.IpV4,
                    joinAllocation.RelayServer.Port,
                    out bool isSecure);

                transport.SetClientRelayData(
                    endpoint.Address.Split(':')[0],
                    endpoint.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData,
                    isSecure);

                Debug.Log("Relay data configured successfully");
                NetworkManager.Singleton.StartClient();

                string hostRole = lobby.Data.ContainsKey("HostRole") ?
                    lobby.Data["HostRole"].Value : "Deck";

                bool clientIsDeck = hostRole != "Deck";

                lobbySearchField.interactable = true;
                btnCreateLobby.interactable = true;
                btnBack.interactable = true;
                pnlPassword.SetActive(false);

                LobbyRoomUI.Instance.OpenLobbyRoom(lobby, clientIsDeck);
            }
            catch (RelayServiceException relayEx)
            {
                Debug.LogError($"Relay join failed: {relayEx.Message} (Code: {relayEx.ErrorCode})");
                await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message} (Error: {e.ErrorCode})");

            // Reset UI
            lobbySearchField.interactable = true;
            btnCreateLobby.interactable = true;
            btnBack.interactable = true;
        }
    }

    private async void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsClient) return;

        Debug.Log("Client fully connected to server!");

        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobbyID);

        /*string hostRole = currentLobby.Data["HostRole"].Value;
        bool clientIsDeck = hostRole != "Deck";

        LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, clientIsDeck);*/

        string hostRole = lobby.Data["HostRole"].Value;
        bool clientIsDeck = hostRole != "Deck";

        lobbySearchField.interactable = true;
        btnCreateLobby.interactable = true;
        btnBack.interactable = true;
        pnlPassword.SetActive(false);
        pnlFailedToJoin.SetActive(false);

        LobbyRoomUI.Instance.OpenLobbyRoom(lobby, clientIsDeck);

        // Unsubscribe so it doesn't stack next joins
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    public async void SubmitPassword()
    {
        /*try
        {
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(selectedLobbyID);

            string realPassword = lobby.Data["Password"].Value;

            if (txtPassword.text != realPassword)
            {
                btnClose.interactable = false;
                btnSubmit.interactable = false;
                pnlFailedToJoin.SetActive(true);

                Debug.LogWarning("Password is required to join this lobby.");
                return;
            }

            JoinLobbyDirectly();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }*/

        if (string.IsNullOrWhiteSpace(selectedLobbyID))
        {
            Debug.LogError("No lobby selected!");
            return;
        }

        try
        {
            // Get lobby to verify password
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(selectedLobbyID);

            // Check if password exists
            if (!lobby.Data.ContainsKey("Password"))
            {
                Debug.LogError("Lobby has no password data!");
                return;
            }

            string realPassword = lobby.Data["Password"].Value;

            if (txtPassword.text != realPassword)
            {
                btnClose.interactable = false;
                btnSubmit.interactable = false;
                pnlFailedToJoin.SetActive(true);
                pnlPassword.SetActive(false);
                Debug.LogWarning("Incorrect password!");
                return;
            }

            // Password correct - get the join code NOW while we have the lobby data
            string joinCode = lobby.Data.ContainsKey("JoinCode") ? lobby.Data["JoinCode"].Value : null;

            // Close password panel
            pnlPassword.SetActive(false);

            await Task.Delay(1500);

            // Join directly, but pass the join code so we don't need another GetLobbyAsync
            JoinLobbyDirectly(prefetchedJoinCode: joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to verify password: {e.Message}");

            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogError("Lobby no longer exists!");
                ClosePasswordPanel();
            }
        }
    }

    public void ClosePasswordPanel()
    {        
        pnlPassword.SetActive(false);
        lobbySearchField.interactable = true;
        btnCreateLobby.interactable = true;
        btnBack.interactable = true;
    }

    public void CloseFailedToJoinPanel()
    {
        pnlFailedToJoin.SetActive(false);
        pnlPassword.SetActive(true);
        btnClose.interactable = true;
        btnSubmit.interactable = true;
    }

    private async void ShowLobbies()
    {
        if (isRefreshingLobbyList) return;

        isRefreshingLobbyList = true;

        while (Application.isPlaying && !shouldStopLobbyList && lobbyListParent != null && lobbyListParent.activeInHierarchy)
        {
            try
            {
                //Debug.Log("Refreshing lobbies...");
                QueryResponse queryResponse =
                    await LobbyService.Instance.QueryLobbiesAsync();

                Debug.Log($"Found {queryResponse.Results.Count} lobbies");

                if (Application.isPlaying)
                {
                    foreach (Transform t in lobbyContentParent)
                    {
                        Destroy(t.gameObject);
                    }
                }

                foreach (Lobby lobby in queryResponse.Results)
                {
                    Debug.Log($"Adding lobby to list - ID: {lobby.Id}, Name: {lobby.Name}, Players: {lobby.Players.Count}/{lobby.MaxPlayers}");
                    Debug.Log($"Lobby has JoinCode: {lobby.Data?.ContainsKey("JoinCode")}");

                    if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
                    {
                        Debug.Log($"JoinCode value: {lobby.Data["JoinCode"].Value}");
                    }

                    Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParent, false);
                    Debug.Log($"Prefab name: {lobbyItemPrefab.name}");
                    Debug.Log($"Instantiated name: {newLobbyItem.name}");
                    Debug.Log($"Child count: {newLobbyItem.childCount}");

                    JoinLobbyButton joinButton =
                        newLobbyItem.GetComponent<JoinLobbyButton>();

                    if (joinButton == null)
                    {

                        //joinButton.lobbyID = lobby.Id;
                        //joinButton.needPassword =
                            //lobby.Data != null && lobby.Data.ContainsKey("HasPassword");
                        Debug.Log($"Set join button - ID: {lobby.Id}, NeedsPassword: {joinButton.needPassword}");
                    }
                    if (joinButton == null)
                    {
                        Debug.LogWarning("JoinLobbyButton not found, adding programmatically");
                        joinButton = newLobbyItem.gameObject.AddComponent<JoinLobbyButton>();
                    }

                    joinButton.lobbyID = lobby.Id;
                    joinButton.needPassword = lobby.Data != null && lobby.Data.ContainsKey("HasPassword");

                    // Verify it was set
                    Debug.Log($"JoinButton set - LobbyID: '{joinButton.lobbyID}', NeedPassword: {joinButton.needPassword}");

                    //newLobbyItem.GetComponent<JoinLobbyButton>().lobbyID = lobby.Id;
                    //newLobbyItem.GetComponent<JoinLobbyButton>().needPassword = lobby.Data != null && lobby.Data.ContainsKey("Password");

                    // Child 0 = Lobby Name
                    newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text =
                        lobby.Name;

                    // Child 1 = Privacy Status
                    newLobbyItem.GetChild(1).GetComponent<TextMeshProUGUI>().text =
                        lobby.Data != null && lobby.Data.ContainsKey("HasPassword")
                        ? "Private"
                        : "Public";

                    // Child 2 = Player Count
                    newLobbyItem.GetChild(2).GetComponent<TextMeshProUGUI>().text =
                            lobby.Players.Count + "/" + lobby.MaxPlayers;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to query lobbies: " + e.Message);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error refreshing lobby list: {e.Message}");
                break; // Break on errors
            }

            await Task.Delay(1000); // Refresh every 1 second
        }

        isRefreshingLobbyList = false;
    }

    public void OpenLobbyCreationPanel()
    {
        lobbyListParent.SetActive(false);
        lobbyCreationParent.SetActive(true);
    }

    public void ExitLobbyCreationButton()
    {
        lobbyCreationParent.SetActive(false);
        lobbyListParent.SetActive(true);
        ShowLobbies();
    }

    public void TogglePasswordField()
    {
        createLobbyPasswordField.interactable = isPrivate.isOn;

        if (!isPrivate.isOn)
        {
            createLobbyPasswordField.text = "1234";
        }
    }

    public async void CreateLobby()
    {
        string lobbyName = createLobbyNameField.text;
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            lobbyName = "Test Lobby";
        }

        bool privateLobby = isPrivate.isOn;

        string hostRole = Random.Range(0, 100) < 50 ? "Deck" : "Cabin";
        try
        {
            Debug.Log("Creating relay allocation...");
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(1);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    {
                        "HostRole",
                        new DataObject(
                            DataObject.VisibilityOptions.Public,
                            hostRole
                        )
                    },
                    {
                        "JoinCode",
                        new DataObject(
                            DataObject.VisibilityOptions.Public,
                            joinCode
                        )
                    }
                }
            };

            Debug.Log("HOST JOIN CODE: " + joinCode);

            if (privateLobby)
            {
                lobbyPassword = createLobbyPasswordField.text;

                if (string.IsNullOrWhiteSpace(lobbyPassword))
                {
                    lobbyPassword = "1234";
                }

                options.Data.Add
                (
                    "HasPassword",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        "true"
                    )
                );

                options.Data.Add
                (
                    "Password",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        lobbyPassword
                    )
                );
            }

            Debug.Log($"Creating lobby: {lobbyName}, MaxPlayers: 2");
            Lobby createdLobby =
                await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                2,
                options
            );

            //createdLobby = await LobbyService.Instance.CreateLobbyAsync("Test Lobby", 2);//, new CreateLobbyOptions { IsPrivate = false });                      

            if (createdLobby == null)
            {
                Debug.LogError("CreateLobbyAsync returned null!");
                return;
            }

            joinedLobbyID = createdLobby.Id;
            currentLobby = createdLobby;

            Debug.Log($"LOBBY CREATED SUCCESSFULLY");
            Debug.Log($"Lobby ID: {createdLobby.Id}");
            Debug.Log($"Lobby Name: {createdLobby.Name}");
            Debug.Log($"Host ID: {createdLobby.HostId}");
            Debug.Log($"Join Code in data: {createdLobby.Data["JoinCode"].Value}");

            UnityTransport transport = null;

            // Try method 1: Get from NetworkManager
            if (NetworkManager.Singleton != null)
            {
                transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                // If still null, try getting from children
                if (transport == null)
                {
                    transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();
                }

                // If still null, check if it's using the default transport
                if (transport == null && NetworkManager.Singleton.NetworkConfig != null)
                {
                    transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
                }
            }

            // Try method 2: Find it anywhere in the scene
            if (transport == null)
            {
                transport = FindObjectOfType<UnityTransport>();
            }

            if (transport == null)
            {
                Debug.LogError("UnityTransport STILL not found! Attempting to add it...");

                // If NetworkManager exists, add UnityTransport to it
                if (NetworkManager.Singleton != null)
                {
                    transport = NetworkManager.Singleton.gameObject.AddComponent<UnityTransport>();

                    // Configure default UnityTransport settings
                    transport.SetConnectionData("127.0.0.1", 7777); // Default values, will be overridden by relay
                }
                else
                {
                    Debug.LogError("NetworkManager.Singleton is null! Make sure you have a NetworkManager in the scene.");
                    return;
                }
            }

            Debug.Log($"UnityTransport found and configured: {transport != null}");
            Debug.Log($"Transport type: {transport.GetType().Name}");

            // Now set the relay data
            /*transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                true
            );*/

            if (transport != null)
            {
                var endpoint = GetEndpointForAllocation(
                    allocation.ServerEndpoints,
                    allocation.RelayServer.IpV4,
                    allocation.RelayServer.Port,
                    out bool isSecure);

                transport.SetHostRelayData(
                    endpoint.Address.Split(':')[0],
                    endpoint.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    isSecure);

                Debug.Log("Host relay data configured (isSecure: " + isSecure + ")");
                NetworkManager.Singleton.StartHost();
            }

            //NetworkManager.Singleton.StartHost();

            /*UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    true
                );

                Debug.Log("Starting host...");
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                Debug.LogError("UnityTransport not found!");
            }*/

            Debug.Log("Lobby created with ID: " + createdLobby.Id);

            lobbyCreationParent.SetActive(false);
            lobbyListParent.SetActive(true);

            string hostFinalRole = createdLobby.Data["HostRole"].Value;
            bool hostIsDeck = hostFinalRole == "Deck";

            LobbyRoomUI.Instance.OpenLobbyRoom(createdLobby, hostIsDeck);

            LobbyHeartBeat(createdLobby);

            //ShowLobbies();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error creating lobby: {e.Message}");
            Debug.LogError($"Stack: {e.StackTrace}");
        }

        //if (!Application.isPlaying)
        //LobbyHeartBeat(currentLobby);

        //AssignRoles(true);

        //LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby);//, AuthenticationService.Instance.Player);

        /*string lobbyName = createLobbyNameField.text;
        bool isPrivate = createLobbyPrivacyDropdown.value == 1;
        try
        {
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, new CreateLobbyOptions { IsPrivate = isPrivate });
            Debug.Log("Lobby created with ID: " + lobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }*/
    }

    private async void LobbyHeartBeat(Lobby lobby)
    {
        while (!shouldStopHeartbeat && Application.isPlaying)
        {
            if (lobby == null) return;

            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            Debug.Log("Sent heartbeat ping for lobby ID: " + lobby.Id);
            await Task.Delay(15*1000); // Send heartbeat every 5 seconds
        }
    }

    public void ResetForNewLobby()
    {
        shouldStopHeartbeat = false;
        shouldStopLobbyList = false;
        currentLobby = null;
        joinedLobbyID = null;

        // Restart the lobby list refreshing if the list is active
        if (lobbyListParent != null && lobbyListParent.activeInHierarchy)
        {
            ShowLobbies();
        }
    }

    public void StopAllLobbyProcesses()
    {
        shouldStopHeartbeat = true;
        shouldStopLobbyList = true;
        isRefreshingLobbyList = false;
    }

    private void OnDestroy()
    {
        StopAllLobbyProcesses();
        Instance = null;
    }

    private async void OnApplicationQuit()
    {
        if (currentLobby == null)
            return;

        if (string.IsNullOrWhiteSpace(currentLobby.Id))
            return;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
        catch { }
    }


    /*private Lobby joinedLobby;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(Random.Range(0, 1000).ToString()); // Optional: Set a profile name for player data persistence

            await UnityServices.InitializeAsync(options);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, new CreateLobbyOptions { IsPrivate = isPrivate });
        } catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    /*public NetworkVariable<int> readyCount = new NetworkVariable<int>(0);

    public void ReadyUp()
    {
        SubmitReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        readyCount.Value++;

        Debug.Log("Player ready. Total: " + readyCount.Value);

        if (readyCount.Value >= NetworkManager.Singleton.ConnectedClientsList.Count)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        Debug.Log("All players ready. Starting game!");

        NetworkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void OnReadyButtonClicked()
    {
        ReadyUp();
    }*/
}
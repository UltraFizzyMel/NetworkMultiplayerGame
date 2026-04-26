using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
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
    //[SerializeField] private bool correctPassword = false;
    private string selectedLobbyID;
    private bool selectedLobbyNeedsPassword;

    //private Player playerData;
    private bool isDeck;
    //private NetworkVariable<bool> isHostDeck = new NetworkVariable<bool>();
    public Lobby currentLobby;
    public string joinedLobbyID;

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
            pnlPassword.SetActive(true);
            return;
            /*try
            {
                if (correctPassword)
                {
                    Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

                    currentLobby = lobby;
                    joinedLobbyID = lobbyID;

                    //AssignRoles(false);
                    string hostRole = currentLobby.Data["HostRole"].Value;
                    bool clientIsDeck = hostRole != "Deck";

                    lobbySearchField.interactable = true;
                    btnCreateLobby.interactable = true;

                    LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, clientIsDeck);
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to join lobby: " + e.Message);
            }*/
        }

        JoinLobbyDirectly();
        /*else
        {
            try
            {
                Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);

                currentLobby = lobby;
                joinedLobbyID = lobbyID;

                //AssignRoles(false);
                string hostRole = currentLobby.Data["HostRole"].Value;
                bool clientIsDeck = hostRole != "Deck";

                LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, clientIsDeck);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError("Failed to join lobby: " + e.Message);
            }
        }
        
        //LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby);//, AuthenticationService.Instance.Player);*/
    }

    private async void JoinLobbyDirectly()
    {
        try
        {
            if (selectedLobbyID == null) return;

            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobbyID);

            currentLobby = lobby;
            joinedLobbyID = lobby.Id;

            string hostRole = currentLobby.Data["HostRole"].Value;
            bool clientIsDeck = hostRole != "Deck";

            lobbySearchField.interactable = true;
            btnCreateLobby.interactable = true;
            pnlPassword.SetActive(false);

            LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, clientIsDeck);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    public async void SubmitPassword()
    {
        try
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
            /*else if (txtPassword.text == lobbyPassword)
            {
                correctPassword = true;
                JoinLobby(joinedLobbyID, true);
            }*/

            JoinLobbyDirectly();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }

    public void ClosePasswordPanel()
    {        
        pnlPassword.SetActive(false);
        lobbySearchField.interactable = true;
        btnCreateLobby.interactable = true;
    }

    public void CloseFailedToJoinPanel()
    {
        pnlFailedToJoin.SetActive(false);
        btnClose.interactable = true;
        btnSubmit.interactable = true;
    }

    private async void ShowLobbies()
    {
        if (isRefreshingLobbyList) return;

        isRefreshingLobbyList = true;

        while (Application.isPlaying && lobbyListParent.activeInHierarchy)
        {
            Debug.Log("Refreshing lobbies...");
            QueryResponse queryResponse = 
                await LobbyService.Instance.QueryLobbiesAsync();

            if (Application.isPlaying)
            {
                foreach (Transform t in lobbyContentParent)
                {
                    Destroy(t.gameObject);
                }
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                Transform newLobbyItem = Instantiate(lobbyItemPrefab, lobbyContentParent, false);

                JoinLobbyButton joinButton =
                    newLobbyItem.GetComponent<JoinLobbyButton>();

                joinButton.lobbyID = lobby.Id;
                joinButton.needPassword =
                    lobby.Data != null && lobby.Data.ContainsKey("Password");

                //newLobbyItem.GetComponent<JoinLobbyButton>().lobbyID = lobby.Id;
                //newLobbyItem.GetComponent<JoinLobbyButton>().needPassword = lobby.Data != null && lobby.Data.ContainsKey("Password");

                // Child 0 = Lobby Name
                newLobbyItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = 
                    lobby.Name;

                // Child 1 = Privacy Status
                newLobbyItem.GetChild(1).GetComponent<TextMeshProUGUI>().text =
                    lobby.Data != null && lobby.Data.ContainsKey("Password")
                    ? "Private"
                    : "Public";

                // Child 2 = Player Count
                newLobbyItem.GetChild(2).GetComponent<TextMeshProUGUI>().text =
                        lobby.Players.Count + "/" + lobby.MaxPlayers;
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
                }
            }
        };

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

        //Lobby createdLobby = null;
        try
        {
            Lobby createdLobby = 
                await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                2,
                options
            );

            //createdLobby = await LobbyService.Instance.CreateLobbyAsync("Test Lobby", 2);//, new CreateLobbyOptions { IsPrivate = false });
            joinedLobbyID = createdLobby.Id;
            currentLobby = createdLobby;
            Debug.Log("Lobby created with ID: " + createdLobby.Id);

            lobbyCreationParent.SetActive(false);
            lobbyListParent.SetActive(true);

            string hostFinalRole = currentLobby.Data["HostRole"].Value;
            bool hostIsDeck = hostFinalRole == "Deck";

            LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, hostIsDeck);

            //ShowLobbies();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }

        //currentLobby = createdLobby;
        LobbyHeartBeat(currentLobby);

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

    /*private void AssignRoles(bool isHost)
    {
        if (isHost)
        {
            bool value = Random.Range(0, 100) < 50; // 50% chance to be deck or cabin
            //isHostDeck.Value = value;

            isDeck = value;
            Debug.Log("Host isDeck set to: " + isDeck);

            LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, isDeck);
        }
        else
        {
            ApplyClientRole();
        }
    }*/

    /*public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            ApplyClientRole();
        }

        // Also react if it changes later
        isHostDeck.OnValueChanged += OnHostDeckChanged;
    }*/

    /*private void ApplyClientRole()
    {
        isDeck = !isHostDeck.Value;
        Debug.Log("Client isDeck set to: " + isDeck);

        LobbyRoomUI.Instance.OpenLobbyRoom(currentLobby, isDeck);
    }*/

    /*private void OnHostDeckChanged(bool oldValue, bool newValue)
    {
        if (!IsHost)
        {
            isDeck = !newValue;
        }
    }*/

    private async void LobbyHeartBeat(Lobby lobby)
    {
        while (true && Application.isPlaying)
        {
            if (lobby == null) return;

            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            Debug.Log("Sent heartbeat ping for lobby ID: " + lobby.Id);
            await Task.Delay(15*1000); // Send heartbeat every 5 seconds
        }
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
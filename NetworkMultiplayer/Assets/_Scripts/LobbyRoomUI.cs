using System.Collections.Generic;
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
    public static LobbyRoomUI Instance;

    [SerializeField] private GameObject lobbyRoomPanel;
    //[SerializeField] private Transform playerListParent;

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

    private void Awake()
    {
        Instance = this;
    }

    public void OpenLobbyRoom(Lobby lobby, bool isDeck)//, Unity.Services.Lobbies.Models.Player player)
    {
        currentLobby = lobby;
        lobbyRoomPanel.SetActive(true);

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
                imgDeckOne.SetActive(true);
                imgCabinTwo.SetActive(true);
            }
            else
            {
                imgCabinOne.SetActive(true);
                imgDeckTwo.SetActive(true);
            }
        }
        else
        {
            if (isDeck)
            {
                imgDeckTwo.SetActive(true);
                imgCabinOne.SetActive(true);
            }
            else
            {
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

    /*private void Update()
    {
        if (currentLobby.Players.Count >= 2 && canInteract)
            btnReady.interactable = true;
        else
            btnReady.interactable = false;
    }*/

    public async void ToggleReady()
    {
        isReady = !isReady;
        string newState = isReady ? "true" : "false";            

        var data = new Dictionary<string, PlayerDataObject>
        {
            {
                "Ready",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newState)
            }
        };

        await LobbyService.Instance.UpdatePlayerAsync(
            currentLobby.Id,
            AuthenticationService.Instance.PlayerId,
            new UpdatePlayerOptions { Data = data }
        );

        await Task.Delay(500);
        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

        if (AllPlayersReady() && isHost)
        {
            //NetworkManager.Singleton.StartHost();
            //NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }

        RefreshUI();
    }

    private bool AllPlayersReady()
    {
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

    private async void UpdateLobbyLoop()
    {
        /*while (true)
        {
            //await Task.Delay(2000);
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            RefreshUI();
            //UpdateUI();
        }*/

        if (isUpdatingLobby) return;
        isUpdatingLobby = true;

        while (lobbyRoomPanel != null && lobbyRoomPanel.activeInHierarchy)
        {
            await Task.Delay(7000);

            if (currentLobby == null) break;

            try
            {
                currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                RefreshUI();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning("Lobby refresh failed: " + e.Message);
            }
        }

        isUpdatingLobby = false;
    }

    private void RefreshUI()
    {
        /*foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }*/

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

        /*foreach (var player in currentLobby.Players)
        {
            //GameObject row = new GameObject("PlayerUI");
            //row.transform.SetParent(playerListParent);

            //TextMeshProUGUI txt = row.AddComponent<TextMeshProUGUI>();

            bool ready =
                player.Data != null &&
                player.Data.ContainsKey("Ready") &&
                player.Data["Ready"].Value == "true";

            //txt.text = player.Id + (ready ? " ✔" : " ❌");
        }*/
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

        /*await LobbyService.Instance.RemovePlayerAsync(
            currentLobby.Id,
            AuthenticationService.Instance.PlayerId
        );*/

        if (isHost)
        {
            foreach (var player in currentLobby.Players)
            {
                if (player.Id != AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        currentLobby.Id,
                        player.Id
                    );
                }
            }
            await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
        }
        else
        {
            await LobbyService.Instance.RemovePlayerAsync(
                currentLobby.Id,
                AuthenticationService.Instance.PlayerId
            );            
        }

        lobbyRoomPanel.SetActive(false);
        lobbyListParent.SetActive(true);
    }
}

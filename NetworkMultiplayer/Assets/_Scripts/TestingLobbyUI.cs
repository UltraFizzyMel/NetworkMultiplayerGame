using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;

public class TestingLobbyUI : MonoBehaviour
{
    //[SerializeField] private Button btncreateGame;
    //[SerializeField] private Button btnjoinGame;

    private Lobby hostLobby;
    private float heartbeatTimer;
    private string playerName;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in anonymously with player ID: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = "UltraMae" + UnityEngine.Random.Range(10, 99);
        Debug.Log("Player Name: " + playerName);
    }

    private void Update()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f;
                _ = SendHeartbeat();
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async Task SendHeartbeat()
    {
        if (hostLobby != null)
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            Debug.Log("Sent heartbeat ping for lobby ID: " + hostLobby.Id);
        }
    }

    private async void CreateLobby()
    {
        try {
            string LobbyName = "MyLobby";
            int maxPlayers = 2;
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = true,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "Difficulty", new DataObject(DataObject.VisibilityOptions.Public, "Normal", DataObject.IndexOptions.S1) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
                        
            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
            
            PrintPlayers(hostLobby);
        } catch (LobbyServiceException e) {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, "Normal", QueryFilter.OpOptions.EQ)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["Difficulty"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to list lobbies: " + e.Message);
        }
    }

    private async void JoinLobbyByCode(string lobbyCode)
    {
        //LobbyService.Instance.JoinLobbyByIdAsync("lobbyID").ContinueWith(task =>
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            Debug.Log("Joined Lobby with code: " + lobbyCode);

            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to list lobbies: " + e.Message);
        }
    }

    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
        }
    }

    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        return new Unity.Services.Lobbies.Models.Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }

    private void PrintPlayers(Lobby lobby)
    {
        if (lobby?.Players == null) return;

        Debug.Log("Players in lobby " + lobby.Name + " " + lobby.Data["Difficulty"].Value);

        foreach (Unity.Services.Lobbies.Models.Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    private void UpdateLobbyGameMode(string difficulty)
    {

    }
}

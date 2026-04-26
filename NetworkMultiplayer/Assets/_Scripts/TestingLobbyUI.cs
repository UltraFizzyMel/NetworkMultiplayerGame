using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;
using System.Threading.Tasks;

public class TestingLobbyUI : MonoBehaviour
{
    //[SerializeField] private Button btncreateGame;
    //[SerializeField] private Button btnjoinGame;

    private Lobby hostLobby;
    private float heartbeatTimer;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in anonymously with player ID: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f;
                SendHeartbeat();
            }
        }
    }

    private void HandleLobbyHeartbeat()
    {
        heartbeatTimer = 15f;
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
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, maxPlayers);

            hostLobby = lobby;

            Debug.Log("Lobby created with ID: " + lobby.Id);
        } catch (LobbyServiceException e) {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to list lobbies: " + e.Message);
        }
    }
}

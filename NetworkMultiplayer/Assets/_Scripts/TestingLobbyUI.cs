using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour
{
    //[SerializeField] private Button btncreateGame;
    //[SerializeField] private Button btnjoinGame;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in anonymously with player ID: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void CreateLobby()
    {
        try {
            string LobbyName = "MyLobby";
            int maxPlayers = 2;
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(LobbyName, maxPlayers);

            Debug.Log("Lobby created with ID: " + lobby.Id);
        } catch (LobbyServiceException e) {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }

    private async void ListLobbies()
    {
        //await Lobbies.Instance.QueryLobbiesAsync();
    }
}

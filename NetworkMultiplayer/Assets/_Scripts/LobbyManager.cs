using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyManager : NetworkBehaviour
{
    private Lobby joinedLobby;


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
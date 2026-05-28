using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ReadyManager : NetworkBehaviour
{
    public static ReadyManager Instance;

    //private NetworkVariable<bool> hostReady = new(false);
    //private NetworkVariable<bool> clientReady = new(false);

    private NetworkVariable<bool> hostReady = new(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    private NetworkVariable<bool> clientReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool gameStarting;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        hostReady.OnValueChanged += OnReadyStateChanged;
        clientReady.OnValueChanged += OnReadyStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        hostReady.OnValueChanged -= OnReadyStateChanged;
        clientReady.OnValueChanged -= OnReadyStateChanged;
    }

    public void ToggleReady()
    {
        SubmitReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitReadyServerRpc(ulong clientId)
    {
        bool isHost = clientId == NetworkManager.ServerClientId;

        if (isHost)
            hostReady.Value = !hostReady.Value;
        else
            clientReady.Value = !clientReady.Value;

        Debug.Log($"HOST READY: {hostReady.Value} | CLIENT READY: {clientReady.Value}");

        if (hostReady.Value && clientReady.Value && !gameStarting)
        {
            gameStarting = true;
            StartGame();
        }
    }

    private void OnReadyStateChanged(bool previous, bool current)
    {
        LobbyRoomUI.Instance?.RefreshReadyVisuals(
            hostReady.Value,
            clientReady.Value
        );
    }

    private async void StartGame()
    {
        Debug.Log("[READY] Both players ready.");

        //await System.Threading.Tasks.Task.Delay(1000);

        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}
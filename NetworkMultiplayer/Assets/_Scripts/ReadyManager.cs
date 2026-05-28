using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ReadyManager : NetworkBehaviour
{
    public static ReadyManager Instance;

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
    public bool IsGameStarting => gameStarting;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        hostReady.OnValueChanged += OnReadyStateChanged;
        clientReady.OnValueChanged += OnReadyStateChanged;

        // Force initial UI refresh
        OnReadyStateChanged(false, false);
    }

    public override void OnNetworkDespawn()
    {
        hostReady.OnValueChanged -= OnReadyStateChanged;
        clientReady.OnValueChanged -= OnReadyStateChanged;
    }

    public void ToggleReady()
    {
        if (gameStarting)
            return;

        SubmitReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitReadyServerRpc(ulong clientId)
    {
        if (gameStarting)
            return;

        bool isHost = clientId == NetworkManager.ServerClientId;

        if (isHost)
            hostReady.Value = !hostReady.Value;
        else
            clientReady.Value = !clientReady.Value;

        Debug.Log($"HOST READY: {hostReady.Value} | CLIENT READY: {clientReady.Value}");

        if (hostReady.Value && clientReady.Value)
        {
            gameStarting = true;

            // Lock in ready state
            hostReady.Value = true;
            clientReady.Value = true;

            StartCoroutine(StartGameRoutine());
        }
    }

    /*private void OnReadyStateChanged(bool previous, bool current)
    {
        LobbyRoomUI.Instance?.RefreshReadyVisuals(
            hostReady.Value,
            clientReady.Value
        );
    }*/

    private void OnReadyStateChanged(bool previous, bool current)
    {
        LobbyRoomUI.Instance?.RefreshReadyVisuals(
            hostReady.Value,
            clientReady.Value
        );

        // Disable buttons once both are ready
        if (hostReady.Value && clientReady.Value)
        {
            LobbyRoomUI.Instance?.LockLobbyUI();
        }
    }

    private IEnumerator StartGameRoutine()
    {
        Debug.Log("[READY] Both players ready.");

        // VERY IMPORTANT:
        // Give NGO time to flush replication/state updates
        yield return new WaitForSeconds(1.0f);

        // Extra safety:
        // wait one full network tick/frame
        yield return null;

        if (!IsServer)
            yield break;

        if (NetworkManager.Singleton == null)
            yield break;

        // Make sure both clients STILL exist
        if (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
        {
            Debug.LogError("[READY] Lost client before scene transition.");
            gameStarting = false;
            ResetReadyStates();
            yield break;
        }

        Debug.Log("[READY] Loading GameScene...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    public void ResetReadyStates()
    {
        if (!IsServer)
            return;

        hostReady.Value = false;
        clientReady.Value = false;

        gameStarting = false;

        Debug.Log("[ReadyManager] Ready states reset.");
    }
}
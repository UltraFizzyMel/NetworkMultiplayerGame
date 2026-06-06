using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("UI")]
    [SerializeField] private GameObject loadingOverlay;
    public GameObject LoadingOverlay => loadingOverlay;

    public Transform DeckSpawn => deckSpawn;
    public Transform CabinSpawn => cabinSpawn;

    public bool PlayersSpawned { get; private set; }

    private readonly NetworkVariable<bool> netIsHostDeck =
        new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public bool HostIsDeck => netIsHostDeck.Value;

    private readonly NetworkVariable<bool> _gameReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool GameReady() => _gameReady.Value;

    private readonly NetworkVariable<int> _tutorialReadyCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private const int PhysicsSettleFrames = 8;
    private const int RequiredPlayers = 2;

    [SerializeField] private GameObject Radio;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        Radio.SetActive(false);

        if (loadingOverlay != null)
            loadingOverlay.SetActive(true);

        if (!IsServer)
            return;

        netIsHostDeck.Value =
            SessionData.Instance != null &&
            SessionData.Instance.isHostDeck;

        StartCoroutine(SpawnPlayersRoutine());
    }

    private IEnumerator SpawnPlayersRoutine()
    {
        // Wait a little after scene load
        for (int i = 0; i < PhysicsSettleFrames; i++)
            yield return new WaitForFixedUpdate();

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool isHostPlayer = clientId == NetworkManager.ServerClientId;

            bool isDeck = isHostPlayer ? netIsHostDeck.Value : !netIsHostDeck.Value;

            Transform spawn = isDeck ? deckSpawn : cabinSpawn;

            NetworkObject player =
                Instantiate(playerPrefab, spawn.position, spawn.rotation);

            player.SpawnAsPlayerObject(clientId, true);

            Player playerScript = player.GetComponent<Player>();

            if (playerScript != null)
            {
                playerScript.SetRole(isDeck);
                playerScript.SpawnPlayerClientRpc(spawn.position, spawn.rotation, isDeck);
            }
        }

        for (int i = 0; i < PhysicsSettleFrames; i++)
            yield return new WaitForFixedUpdate();

        //_gameReady.Value = true;
        PlayersSpawned = true;
        Debug.Log("[GameManager] All players spawned.");

        //HideLoadingOverlayClientRpc();
    }

    // ─── Tutorial completion ─────────────────────────────────────────────────
    public void ReportTutorialComplete()
    {
        ReportTutorialCompleteRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportTutorialCompleteRpc()
    {
        _tutorialReadyCount.Value++;
        Debug.Log($"[GameManager] Tutorial ready: {_tutorialReadyCount.Value}/{RequiredPlayers}");

        if (_tutorialReadyCount.Value >= RequiredPlayers)
        {
            _gameReady.Value = true;
            Radio.SetActive(true);
            HideLoadingOverlayClientRpc();
            DisableCursorClientRpc();
            Debug.Log("[GameManager] Both players ready — game starting.");
        }
    }

    [ClientRpc]
    private void DisableCursorClientRpc()
    {
        foreach (Player player in PlayerRegistry.Players)
        {
            if (!player.IsOwner)
                continue;

            player.DisableCursor();
            break;
        }
    }

    [ClientRpc]
    private void HideLoadingOverlayClientRpc()
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }
}
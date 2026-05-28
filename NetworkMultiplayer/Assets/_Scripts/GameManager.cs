// ─── GameManager.cs ──────────────────────────────────────────────────────────
/*using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    [Header("UI")]
    [SerializeField] private GameObject loadingOverlay;

    // Expose the overlay so Player.cs can hide it after a safe spawn.
    public GameObject LoadingOverlay => loadingOverlay;

    private readonly NetworkVariable<bool> _netIsHostDeck = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private const int RequiredPlayers = 2;
    private const float SetupTimeout = 30f;
    // Extra fixed-update frames to wait after players are found.
    // In a build the physics engine needs several fixed steps to register
    // all scene colliders before a CharacterController can land on them.
    private const int PhysicsSettleFrames = 6;

    // ─── Network spawn ───────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // Keep the overlay up on every client until we confirm the teleport
        // has actually completed on that client (see HideOverlayClientRpc).
        if (loadingOverlay != null) loadingOverlay.SetActive(true);

        if (!IsServer) return;

        _netIsHostDeck.Value =
            SessionData.Instance != null && SessionData.Instance.isHostDeck;

        StartCoroutine(SetupRoutine());
    }

    // ─── Setup (server only) ─────────────────────────────────────────────────

    private IEnumerator SetupRoutine()
    {
        // 1. Wait for both clients to be fully connected -----------------------
        float elapsed = 0f;
        while (NetworkManager.Singleton.ConnectedClientsList.Count < RequiredPlayers)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= SetupTimeout)
            {
                Debug.LogError("[GameManager] Timed out waiting for clients.");
                yield break;
            }
            yield return null;
        }

        // 2. Wait for both Player NetworkObjects to be spawned -----------------
        elapsed = 0f;
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        while (players.Length < RequiredPlayers)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= SetupTimeout)
            {
                Debug.LogError("[GameManager] Timed out waiting for Player objects.");
                yield break;
            }
            yield return null;
            players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        }

        // 3. Let physics run for several fixed steps so colliders register -----
        //    This is the critical fix: WaitForFixedUpdate advances the physics
        //    simulation, which WaitForEndOfFrame / yield return null do NOT.
        for (int i = 0; i < PhysicsSettleFrames; i++)
            yield return new WaitForFixedUpdate();

        // 4. Send each player to their spawn point ----------------------------
        //    The overlay is hidden from INSIDE SpawnPlayerClientRpc on the
        //    client AFTER the physics-safe teleport completes, not here.
        AssignSpawnPositions(players);
    }

    private void AssignSpawnPositions(Player[] players)
    {
        foreach (Player player in players)
        {
            bool isHostPlayer = player.OwnerClientId == NetworkManager.ServerClientId;
            bool playerIsDeck = isHostPlayer ? _netIsHostDeck.Value : !_netIsHostDeck.Value;
            Transform spawn = playerIsDeck ? deckSpawn : cabinSpawn;

            // Pass hideOverlayAfter = true so the client knows to close the
            // loading screen once it has finished repositioning itself.
            player.SpawnPlayerClientRpc(spawn.position, spawn.rotation, playerIsDeck);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}*/

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

    private readonly NetworkVariable<bool> netIsHostDeck =
        new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        Instance = this;

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
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool isHostPlayer =
                clientId == NetworkManager.ServerClientId;

            bool isDeck =
                isHostPlayer
                ? netIsHostDeck.Value
                : !netIsHostDeck.Value;

            Transform spawn =
                isDeck
                ? deckSpawn
                : cabinSpawn;

            NetworkObject player =
                Instantiate(
                    playerPrefab,
                    spawn.position,
                    spawn.rotation
                );

            player.SpawnAsPlayerObject(clientId, true);

            Player playerScript = player.GetComponent<Player>();

            if (playerScript != null)
            {
                playerScript.ApplyRoleVisualsClientRpc(isDeck);
            }
        }

        HideLoadingOverlayClientRpc();
    }

    [ClientRpc]
    private void HideLoadingOverlayClientRpc()
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
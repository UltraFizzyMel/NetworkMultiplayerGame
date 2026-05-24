// ─── GameManager.cs ──────────────────────────────────────────────────────────
using System.Collections;
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
}

// ─── GameManager.cs ──────────────────────────────────────────────────────────
/*using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    [Header("Player")]
    [SerializeField] private NetworkObject playerPrefab; // Drag your Player prefab here

    [Header("UI")]
    [SerializeField] private GameObject loadingOverlay;

    private readonly NetworkVariable<bool> _netIsHostDeck = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private const int RequiredPlayers = 2;
    private const float SetupTimeout = 60f;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[GameManager] OnNetworkSpawn - IsServer: {IsServer}");

        if (loadingOverlay != null) loadingOverlay.SetActive(true);

        if (!IsServer) return;

        _netIsHostDeck.Value = SessionData.Instance != null && SessionData.Instance.isHostDeck;
        StartCoroutine(SetupRoutine());
    }

    private IEnumerator SetupRoutine()
    {
        Debug.Log("[GameManager] Setup started, waiting for clients...");

        // 1. Wait for all clients to connect
        float elapsed = 0f;
        while (NetworkManager.Singleton.ConnectedClientsList.Count < RequiredPlayers)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= SetupTimeout)
            {
                Debug.LogError($"[GameManager] Timeout! Connected: {NetworkManager.Singleton.ConnectedClientsList.Count}/{RequiredPlayers}");
                yield break;
            }
            yield return null;
        }

        Debug.Log($"[GameManager] All {RequiredPlayers} clients connected! Spawning players...");

        // 2. Spawn player for each client
        List<Player> spawnedPlayers = new List<Player>();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[GameManager] Player prefab is not assigned in the inspector!");
                continue;
            }

            NetworkObject playerObj = Instantiate(playerPrefab);
            playerObj.SpawnAsPlayerObject(client.ClientId);

            Debug.Log($"[GameManager] Spawned player for client {client.ClientId}");

            Player player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                spawnedPlayers.Add(player);
            }
        }

        // 3. Wait for all players to be fully spawned on all clients
        yield return new WaitForSeconds(1f);

        // Verify all players are spawned
        elapsed = 0f;
        while (spawnedPlayers.Count < RequiredPlayers)
        {
            elapsed += 0.5f;
            if (elapsed >= SetupTimeout)
            {
                Debug.LogError("[GameManager] Timeout waiting for player spawns!");
                yield break;
            }

            // Re-check spawned players
            spawnedPlayers.Clear();
            Player[] allPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player p in allPlayers)
            {
                if (p.IsSpawned)
                {
                    spawnedPlayers.Add(p);
                }
            }

            Debug.Log($"[GameManager] Waiting for players... Found: {spawnedPlayers.Count}/{RequiredPlayers}");
            yield return new WaitForSeconds(0.5f);
        }

        yield return null;
        yield return null;

        // 4. Assign spawn positions
        AssignSpawnPositions(spawnedPlayers.ToArray());

        // 5. Hide loading overlay
        yield return new WaitForSeconds(0.5f);
        HideLoadingOverlayClientRpc();

        Debug.Log("[GameManager] Setup complete!");
    }

    private void AssignSpawnPositions(Player[] players)
    {
        Debug.Log($"[GameManager] Assigning positions for {players.Length} players");

        foreach (Player player in players)
        {
            if (player == null || !player.IsSpawned)
            {
                Debug.LogError("[GameManager] Cannot assign - player is null or not spawned");
                continue;
            }

            bool isHostPlayer = player.OwnerClientId == NetworkManager.ServerClientId;
            bool playerIsDeck = isHostPlayer ? _netIsHostDeck.Value : !_netIsHostDeck.Value;
            Transform spawn = playerIsDeck ? deckSpawn : cabinSpawn;

            if (spawn == null)
            {
                Debug.LogError($"[GameManager] Spawn point missing for {(playerIsDeck ? "Deck" : "Cabin")}");
                continue;
            }

            Debug.Log($"[GameManager] Player {player.OwnerClientId} (Host:{isHostPlayer}) -> {(playerIsDeck ? "Deck" : "Cabin")}");

            // Set position on server
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawn.position;
            player.transform.rotation = spawn.rotation;

            if (player.crew != null) player.crew.SetActive(playerIsDeck);
            if (player.captain != null) player.captain.SetActive(!playerIsDeck);

            if (cc != null) cc.enabled = true;

            // Tell non-owner clients to update
            if (!player.IsOwner)
            {
                player.UpdateVisualsClientRpc(spawn.position, spawn.rotation, playerIsDeck);
            }
        }
    }

    [ClientRpc]
    private void HideLoadingOverlayClientRpc()
    {
        Debug.Log("[GameManager] Hiding loading overlay");
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
    }
}*/
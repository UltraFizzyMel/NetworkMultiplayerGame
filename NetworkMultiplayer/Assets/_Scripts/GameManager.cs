// ─── GameManager.cs ──────────────────────────────────────────────────────────
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    [Header("UI")]
    // Create a full-screen Canvas in the game scene, set it active by default.
    // GameManager will hide it once both players are set up.
    [SerializeField] private GameObject loadingOverlay;

    // Synced from the host so every client knows the role assignment.
    private readonly NetworkVariable<bool> _netIsHostDeck = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private const int RequiredPlayers = 2;
    private const float SetupTimeout = 30f;

    // ─── Network spawn ───────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        // Show the loading overlay on every client while the host sets things up
        if (loadingOverlay != null) loadingOverlay.SetActive(true);

        if (!IsServer) return;

        // Push the role assignment to all clients via the NetworkVariable
        _netIsHostDeck.Value = SessionData.Instance != null && SessionData.Instance.isHostDeck;

        StartCoroutine(SetupRoutine());
    }

    // ─── Setup ───────────────────────────────────────────────────────────────

    private IEnumerator SetupRoutine()
    {
        // 1. Wait until both NGO clients are connected
        float elapsed = 0f;
        while (NetworkManager.Singleton.ConnectedClientsList.Count < RequiredPlayers)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= SetupTimeout)
            {
                Debug.LogError("[GameManager] Timed out waiting for all clients to connect.");
                yield break;
            }
            yield return null;
        }

        // 2. Wait until both Player components have spawned in the scene
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

        yield return null; // One frame for things to settle

        AssignSpawnPositions(players);

        // Hide the loading overlay on all clients now that setup is done
        HideLoadingOverlayClientRpc();
    }

    private void AssignSpawnPositions(Player[] players)
    {
        foreach (Player player in players)
        {
            bool isHostPlayer = player.OwnerClientId == NetworkManager.ServerClientId;
            bool playerIsDeck = isHostPlayer ? _netIsHostDeck.Value : !_netIsHostDeck.Value;
            Transform spawn = playerIsDeck ? deckSpawn : cabinSpawn;

            player.SpawnPlayerClientRpc(spawn.position, spawn.rotation, playerIsDeck);
        }
    }

    [ClientRpc]
    private void HideLoadingOverlayClientRpc()
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
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
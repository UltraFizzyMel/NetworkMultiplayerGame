using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SwapManager : NetworkBehaviour
{
    public static SwapManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float swapInterval = 30f;
    [SerializeField] private float warningDuration = 5f; // Must be <= swapInterval

    [Header("Valid Swap Zones")]
    [SerializeField] private SwapZone deckZone;
    [SerializeField] private SwapZone cabinZone;

    // Surfaced so the UI countdown can read it (read-only from other scripts)
    public float TimeUntilNextSwap { get; private set; }

    private bool _playerAIsDeck;
    private bool _rolesInitialised = false;
    private bool _swapRunning;

    // ─── Network spawn ───────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (!IsServer) return;
        StartCoroutine(SwapLoop());
    }

    public override void OnNetworkDespawn()
    {
        _swapRunning = false;
        Instance = null;
    }

    // ─── Main loop (server only) ─────────────────────────────────────────────

    private IEnumerator SwapLoop()
    {
        if (_swapRunning) yield break;
        _swapRunning = true;

        // Wait until GameManager finishes spawning everybody
        yield return new WaitUntil(() =>
            GameManager.Instance != null && GameManager.Instance.GameReady());

        while (_swapRunning && Application.isPlaying)
        {
            // ── Wait for two players to be present ──────────────────────────
            yield return new WaitUntil(() => PlayerRegistry.Players.Count >= 2);

            // ── Count down, exposing time for any UI that wants it ───────────
            float waitBeforeWarning = swapInterval - warningDuration;
            TimeUntilNextSwap = swapInterval;

            float elapsed = 0f;
            while (elapsed < waitBeforeWarning)
            {
                elapsed += Time.deltaTime;
                TimeUntilNextSwap = swapInterval - elapsed;
                yield return null;
            }

            // ── Tell every client to start the warning visuals ───────────────
            if (PlayerRegistry.Players.Count >= 2)
            {
                TriggerWarningClientRpc(warningDuration);
                PlayWarningSFXClientRpc();
            }

            elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                TimeUntilNextSwap = warningDuration - elapsed;
                yield return null;
            }

            TimeUntilNextSwap = 0f;

            // ── Swap ─────────────────────────────────────────────────────────
            if (PlayerRegistry.Players.Count >= 2)
                PerformSwap();
        }
    }

    // ─── Role initialisation ─────────────────────────────────────────────────

    private void InitRolesIfNeeded()
    {
        if (_rolesInitialised) return;
        if (GameManager.Instance == null) return;
        if (PlayerRegistry.Players.Count < 2) return;

        Player playerA = PlayerRegistry.Players[0];
        bool playerAIsHost = playerA.OwnerClientId == NetworkManager.ServerClientId;

        _playerAIsDeck = playerAIsHost
            ? GameManager.Instance.HostIsDeck
            : !GameManager.Instance.HostIsDeck;

        _rolesInitialised = true;
        Debug.Log($"[SwapManager] Roles initialised. PlayerA isDeck: {_playerAIsDeck}");
    }

    // ─── Swap logic (server only) ────────────────────────────────────────────

    private void PerformSwap()
    {
        PlayerRegistry.Players.Sort((a, b) => a.NetworkObjectId.CompareTo(b.NetworkObjectId));

        InitRolesIfNeeded();

        Player playerA = PlayerRegistry.Players[0];
        Player playerB = PlayerRegistry.Players[1];

        if (playerA == null || playerB == null)
        {
            Debug.LogWarning("[SwapManager] A player reference is null – skipping swap.");
            return;
        }

        Vector3 rawPosA = playerB.transform.position;
        Vector3 rawPosB = playerA.transform.position;        
        Quaternion rotA = playerB.transform.rotation;
        Quaternion rotB = playerA.transform.rotation;

        //After the swap A moves to the opposite zone, so clamp to that zone
        SwapZone newZoneA = _playerAIsDeck ? cabinZone : deckZone;
        SwapZone newZoneB = _playerAIsDeck ? deckZone : cabinZone;

        Vector3 posA = newZoneA != null ? newZoneA.GetSafePosition(rawPosA) : rawPosA;
        Vector3 posB = newZoneB != null ? newZoneB.GetSafePosition(rawPosB) : rawPosB;

        _playerAIsDeck = !_playerAIsDeck;

        playerA.SetRole(_playerAIsDeck);
        playerB.SetRole(!_playerAIsDeck);

        playerA.TeleportClientRpc(posA, rotA);
        playerB.TeleportClientRpc(posB, rotB);

        // If we want to change visuals for roles
        //playerA.ApplyRoleVisualsClientRpc(_playerAIsDeck);
        //playerB.ApplyRoleVisualsClientRpc(!_playerAIsDeck);

        Debug.Log("[SwapManager] Swap complete.");
    }

    // ─── Client RPCs ─────────────────────────────────────────────────────────
    //Tells every client to find its own local player and start the warning FX.
    [ClientRpc]
    private void TriggerWarningClientRpc(float duration)
    {
        foreach (Player player in PlayerRegistry.Players)
        {
            if (!player.IsOwner) continue;
            player.StartSwapWarning(duration);
            break;
        }
    }

    [ClientRpc]
    private void PlayWarningSFXClientRpc()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlaySFX(SFXType.SwopWarning);
    }
}
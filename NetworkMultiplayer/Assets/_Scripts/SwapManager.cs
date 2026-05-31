// ─── SwapManager.cs ──────────────────────────────────────────────────────────
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SwapManager : NetworkBehaviour
{
    public static SwapManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float swapInterval = 30f;
    [SerializeField] private float warningDuration = 5f; // Must be <= swapInterval

    // Surfaced so the UI countdown can read it (read-only from other scripts)
    public float TimeUntilNextSwap { get; private set; }

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
            GameManager.Instance != null &&
            GameManager.Instance.PlayersSpawned
        );

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

    // ─── Swap logic (server only) ────────────────────────────────────────────

    private void PerformSwap()
    {
        PlayerRegistry.Players.Sort((a, b) =>
            a.NetworkObjectId.CompareTo(b.NetworkObjectId));

        Player playerA = PlayerRegistry.Players[0];
        Player playerB = PlayerRegistry.Players[1];

        if (playerA == null || playerB == null)
        {
            Debug.LogWarning("[SwapManager] A player reference is null – skipping swap.");
            return;
        }

        Vector3 posA = playerA.transform.position;
        Vector3 posB = playerB.transform.position;
        Quaternion rotA = playerA.transform.rotation;
        Quaternion rotB = playerB.transform.rotation;

        playerA.TeleportClientRpc(posB, rotB);
        playerB.TeleportClientRpc(posA, rotA);

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
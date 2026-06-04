using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogZoneManager : MonoBehaviour
{
    public static FogZoneManager Instance { get; private set; }

    [Header("Death Zone")]
    [SerializeField] private float deathZoneTimeout = 10f;
    public static bool BlockForwardMovement { get; private set; }

    private bool _warnLeft, _warnRight;
    private bool _deathLeft, _deathRight;
    private bool _ignoreLeft, _ignoreRight;

    private Coroutine _deathTimer;

    [Header("Outer Fog VFX")]
    [SerializeField] private GameObject leftDeathFog;
    [SerializeField] private GameObject rightDeathFog;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ─── Zone callbacks (called by FogZone.cs) ────────────────────────────────
    public void OnEnterZone(FogZoneType type, bool isRight)
    {
        SetFlag(type, isRight, true);
        UpdateVisualTargets();

        if (type == FogZoneType.IgnoreSteering)
            ApplySteeringBlock(isRight, true);

        // Death timer runs on server only — LoseGame() is already server-guarded
        // so calling it from the client would be a no-op, but keeping it
        // server-only avoids unnecessary calls.
        if (IsServerLocal() && type == FogZoneType.Death)
            StartDeathTimer();
    }

    public void OnExitZone(FogZoneType type, bool isRight)
    {
        SetFlag(type, isRight, false);
        UpdateVisualTargets();

        if (type == FogZoneType.IgnoreSteering)
            ApplySteeringBlock(isRight, false);

        // Stop the timer only if NEITHER side is in the death zone any more
        if (IsServerLocal() && type == FogZoneType.Death && !_deathLeft && !_deathRight)
            StopDeathTimer();
    }

    // ─── Zone flags ───────────────────────────────────────────────────────────
    private void SetFlag(FogZoneType type, bool isRight, bool value)
    {
        switch (type)
        {
            case FogZoneType.Warning:
                if (isRight) _warnRight = value; else _warnLeft = value; break;
            case FogZoneType.Death:
                if (isRight) _deathRight = value; else _deathLeft = value; break;
            case FogZoneType.IgnoreSteering:
                if (isRight) _ignoreRight = value; else _ignoreLeft = value; break;
        }

        BlockForwardMovement = _deathLeft || _deathRight;
        UpdateFogVFX();
    }

    private void UpdateFogVFX()
    {
        if (leftDeathFog != null)
            leftDeathFog.SetActive(_deathLeft);

        if (rightDeathFog != null)
            rightDeathFog.SetActive(_deathRight);
    }

    // ─── Visual targets ───────────────────────────────────────────────────────

    private void UpdateVisualTargets()
    {
        Player localPlayer = FindLocalPlayer();

        if (localPlayer == null) return;

        // Death takes priority over warning
        if (_deathLeft || _deathRight)
            localPlayer.SetFogVisuals(FogVisualLevel.Death);
        else if (_warnLeft || _warnRight)
            localPlayer.SetFogVisuals(FogVisualLevel.Warning);
        else
            localPlayer.SetFogVisuals(FogVisualLevel.None);
    }

    private Player FindLocalPlayer()
    {
        foreach (Player player in PlayerRegistry.Players)
            if (player.IsOwner) return player;

        return null;
    }

    // ─── Steering blocks ──────────────────────────────────────────────────────

    // The bools are set locally on every client. Since EnvironmentMover runs
    // on all clients and moves the zones at the same rate, both clients enter
    // and exit zones simultaneously. BoatSteeringManager.SetSteering reads
    // the bools before sending its ServerRpc, so the block is enforced on the
    // steering player's machine — the only place it needs to be.
    private void ApplySteeringBlock(bool isRight, bool blocked)
    {
        if (BoatSteeringManager.Instance == null) return;
        if (isRight) BoatSteeringManager.Instance.blockRightSteering = blocked;
        else BoatSteeringManager.Instance.blockLeftSteering = blocked;
    }

    public void ReapplySteeringBlocks()
    {
        if (BoatSteeringManager.Instance == null) return;

        // _ignoreLeft/Right track whether each side's IgnoreSteering zone is active.
        // Re-writing them forces the steering manager back to the correct state
        // regardless of when the swap and the trigger happened relative to each other.
        BoatSteeringManager.Instance.blockLeftSteering = _ignoreLeft;
        BoatSteeringManager.Instance.blockRightSteering = _ignoreRight;
    }

    // ─── Death timer ──────────────────────────────────────────────────────────

    private void StartDeathTimer()
    {
        if (_deathTimer != null) return; // Already counting down
        Debug.Log("[FogZoneManager] Death zone entered — timer started.");
        _deathTimer = StartCoroutine(DeathTimerRoutine());
    }

    private void StopDeathTimer()
    {
        if (_deathTimer == null) return;
        StopCoroutine(_deathTimer);
        _deathTimer = null;
        Debug.Log("[FogZoneManager] Death zone exited — timer reset.");
    }

    private IEnumerator DeathTimerRoutine()
    {
        yield return new WaitForSeconds(deathZoneTimeout);
        Debug.Log("[FogZoneManager] Death zone timeout — boat lost in fog.");
        BoatWinLoseController.Instance?.LoseGame();
        _deathTimer = null;
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static bool IsServerLocal() =>
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.IsServer;
}
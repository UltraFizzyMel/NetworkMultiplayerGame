// ─── FogZoneManager.cs ───────────────────────────────────────────────────────
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogZoneManager : MonoBehaviour
{
    public static FogZoneManager Instance { get; private set; }

    [Header("Warning Zone")]
    [Tooltip("Pale cool grey — subtle fog beginning to close in")]
    [SerializeField] private Color warningFogTint = new Color(0.88f, 0.91f, 0.95f, 1f);
    [SerializeField, Range(0f, 1f)] private float warningFilmGrain = 0.2f;

    [Header("Death Zone")]
    [Tooltip("Heavier cool grey — dense fog, danger")]
    [SerializeField] private Color deathFogTint = new Color(0.68f, 0.74f, 0.84f, 1f);
    [SerializeField, Range(0f, 1f)] private float deathFilmGrain = 0.55f;
    [SerializeField] private float deathZoneTimeout = 10f;
    public static bool BlockForwardMovement { get; private set; }

    // ── Volume components ────────────────────────────────────────────────────
    private Color _baseColor;
    private float _baseGrain;

    // ── Zone state (per side) ────────────────────────────────────────────────
    private bool _warnLeft, _warnRight;
    private bool _deathLeft, _deathRight;
    private bool _ignoreLeft, _ignoreRight;

    // ── Death timer (only runs on server) ─────────────────────────────────────
    private Coroutine _deathTimer;

    [Header("Outer Fog VFX")]
    [SerializeField] private GameObject leftDeathFog;
    [SerializeField] private GameObject rightDeathFog;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        // Snap back to base values so effects don't bleed into other scenes
        if (Instance == this) Instance = null;
    }

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
        if (IsServerLocal() && type == FogZoneType.Death
            && !_deathLeft && !_deathRight)
        {
            StopDeathTimer();
        }
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

        if (localPlayer == null)
            return;

        if (_deathLeft || _deathRight)
        {
            localPlayer.SetDeathFogVisuals();
        }
        else if (_warnLeft || _warnRight)
        {
            localPlayer.SetWarningFogVisuals();
        }
        else
        {
            localPlayer.ResetFogVisuals();
        }
    }

    private Player FindLocalPlayer()
    {
        foreach (Player player in PlayerRegistry.Players)
        {
            if (player.IsOwner)
                return player;
        }

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
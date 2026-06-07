using UnityEngine;

public class BackFogManager : MonoBehaviour
{
    public static BackFogManager Instance { get; private set; }

    [Header("Slow Zone")]
    // 0.75 = 75% speed. Serialised here so it's tunable in the Inspector
    // without touching Player.cs.
    [SerializeField] private float slowMultiplier = 0.75f;

    // Read by BackFogMover to stop advancing when the innermost zone hits the boat
    public bool FogStopped => _stopZoneActive;

    private bool _visualZoneActive;
    private bool _slowZoneActive;
    private bool _stopZoneActive;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void OnEnterZone(BackFogZoneType type)
    {
        SetFlag(type, true);
        NotifyLocalPlayer();
    }

    public void OnExitZone(BackFogZoneType type)
    {
        SetFlag(type, false);
        NotifyLocalPlayer();
    }

    private void SetFlag(BackFogZoneType type, bool value)
    {
        switch (type)
        {
            case BackFogZoneType.Visual: _visualZoneActive = value; break;
            case BackFogZoneType.Slow: _slowZoneActive = value; break;
            case BackFogZoneType.Stop: _stopZoneActive = value; break;
        }
    }

    // Runs on every machine independently — each machine's local player gets
    // its own effect applied. This works because BackFogMover reads
    // NetworkVariables (generator fuel, boat speed) so the fog position is
    // deterministic and identical on both machines.
    private void NotifyLocalPlayer()
    {
        foreach (Player player in PlayerRegistry.Players)
        {
            if (!player.IsOwner) continue;
            player.SetBackFogState(
                _visualZoneActive,
                _slowZoneActive ? slowMultiplier : 1f);
            break;
        }
    }
}
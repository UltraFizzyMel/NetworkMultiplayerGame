using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SwapManager : NetworkBehaviour
{
    public static SwapManager Instance { get; private set; }

    [Header("Timing")]
    [SerializeField] private float swapCooldownStart = 20f;
    [SerializeField] private float swapCooldownDuration = 5f;

    [Header("UI")]
    [SerializeField] private Image imgSwap;
    // Inspector-friendly: set Full Alpha colour in the editor (e.g. white, a=1)
    // and Low Alpha colour (same colour, a≈0.06). Beats doing it in code.
    [SerializeField] private Color colCanSwap = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color colCannotSwap = new Color(1f, 1f, 1f, 0.06f); // ~15/255

    // Synced so a late-joining or reconnecting client gets the right state.
    private readonly NetworkVariable<bool> _onCooldown = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private bool _cooldownRunning;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // React to NetworkVariable changes on ALL clients (including late joins).
        _onCooldown.OnValueChanged += OnCooldownChanged;

        // Apply whatever state the server already has when this client spawns.
        ApplyCooldownVisual(_onCooldown.Value);

        if (!IsServer) return;

        StartCoroutine(CooldownRoutine(swapCooldownStart));
    }

    public override void OnNetworkDespawn()
    {
        _onCooldown.OnValueChanged -= OnCooldownChanged;
        Instance = null;
    }

    // ─── Swap (called by UI button on the local client) ──────────────────────

    /// <summary>Call this from your swap button's OnClick.</summary>
    public void TrySwap()
    {
        // Only the server actually executes the swap.
        if (!IsServer) { RequestSwapServerRpc(); return; }
        ServerTrySwap();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSwapServerRpc() => ServerTrySwap();

    private void ServerTrySwap()
    {
        if (_cooldownRunning) return;
        PerformSwap();
        StartCoroutine(CooldownRoutine(swapCooldownDuration));
    }

    // ─── Swap logic (server only) ────────────────────────────────────────────

    private void PerformSwap()
    {
        if (PlayerRegistry.Players.Count < 2)
        {
            Debug.LogWarning("[SwapManager] Not enough players to swap.");
            return;
        }

        PlayerRegistry.Players.Sort((a, b) =>
            a.NetworkObjectId.CompareTo(b.NetworkObjectId));

        Player playerA = PlayerRegistry.Players[0];
        Player playerB = PlayerRegistry.Players[1];

        if (playerA == null || playerB == null) return;

        Vector3 posA = playerA.transform.position, posB = playerB.transform.position;
        Quaternion rotA = playerA.transform.rotation, rotB = playerB.transform.rotation;

        playerA.TeleportClientRpc(posB, rotB);
        playerB.TeleportClientRpc(posA, rotA);

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlaySFX(SFXType.Swop);

        Debug.Log("[SwapManager] Swap complete.");
    }

    // ─── Cooldown (server only, result broadcast via NetworkVariable) ─────────

    private IEnumerator CooldownRoutine(float duration)
    {
        _cooldownRunning = true;
        _onCooldown.Value = true;   // triggers OnCooldownChanged on all clients

        yield return new WaitForSeconds(duration);

        _onCooldown.Value = false;
        _cooldownRunning = false;
    }

    // ─── Visual update (runs on every client) ────────────────────────────────

    // Fired by the NetworkVariable whenever the server changes it.
    private void OnCooldownChanged(bool previous, bool current)
        => ApplyCooldownVisual(current);

    private void ApplyCooldownVisual(bool onCooldown)
    {
        if (imgSwap == null) return;
        imgSwap.color = onCooldown ? colCannotSwap : colCanSwap;
    }
}

/*using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections;

public class SwapManager : NetworkBehaviour
{
    [SerializeField] private Player playerA;
    [SerializeField] private Player playerB;

    [SerializeField] private float timer = 30f;
    private bool assigned = false;
    private float assignDelay = 1f;
    private bool waitingToAssign = true;

    //public NetworkVariable<ulong> controllingClientId =
    //new NetworkVariable<ulong>(ulong.MaxValue);

    private void Awake()
    {
        //NetworkManager.Singleton.StartHost();
        //NetworkManager.Singleton.StartClient();
}
    private void Update()
    {
        if (!IsServer) return;

        if (!assigned && NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            StartCoroutine(AssignAfterDelay());
            assigned = true;
        }
        else
            return;

        /*if (!assigned && NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
        {
            AssignPlayers();
            assigned = true;
        }

        if (!assigned) return;*/

/*if (waitingToAssign)
{
    assignDelay -= Time.deltaTime;
    if (assignDelay <= 0f && NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
    {
        AssignPlayers();
        assigned = true;
        waitingToAssign = false;
    }
    return;
}

timer -= Time.deltaTime;

if (timer <= 0f)
{
    SwapPlayers();
    timer = 30f;
}
}

private IEnumerator AssignAfterDelay()
{
yield return new WaitForSeconds(1f); // give everything time to spawn

AssignPlayers();

Debug.Log("Players assigned AFTER delay");
}

private void AssignPlayers()
{
/*var clients = NetworkManager.Singleton.ConnectedClientsList
    .OrderBy(c => c.ClientId) // Ensure consistent order
    .Select(c => c.ClientId)
    .ToList();

// Shuffle randomly
if (Random.value > 0.5f)
{
    playerA.controllingClientId.Value = clients[0];
    playerB.controllingClientId.Value = clients[1];
}
else
{
    playerA.controllingClientId.Value = clients[1];
    playerB.controllingClientId.Value = clients[0];
}

Debug.Log("Players assigned!");

var clients = NetworkManager.Singleton.ConnectedClientsList
.OrderBy(c => c.ClientId)
.Select(c => c.ClientId)
.ToList();

// Random swap
if (Random.value > 0.5f)
{
    playerA.controllingClientId.Value = clients[0];
    playerB.controllingClientId.Value = clients[1];
}
else
{
    playerA.controllingClientId.Value = clients[1];
    playerB.controllingClientId.Value = clients[0];
}

playerA.controllingClientId.SetDirty(true);
playerB.controllingClientId.SetDirty(true);

Debug.Log($"Client 0: {clients[0]} | Client 1: {clients[1]}");
Debug.Log($"PlayerA -> {playerA.controllingClientId.Value}");
Debug.Log($"PlayerB -> {playerB.controllingClientId.Value}");
}

private void SwapPlayers()
{
ulong temp = playerA.controllingClientId.Value;

playerA.controllingClientId.Value = playerB.controllingClientId.Value;
playerB.controllingClientId.Value = temp;

Debug.Log("Players swapped!");
}
}*/

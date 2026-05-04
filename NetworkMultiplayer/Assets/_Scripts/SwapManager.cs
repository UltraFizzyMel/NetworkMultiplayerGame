using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;

public class SwapManager : NetworkBehaviour
{
    [SerializeField] private float swapInterval = 30f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;
        
        StartCoroutine(SwapLoop());
    }

    private IEnumerator SwapLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(swapInterval);
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlaySFX(SFXType.SwopWarning);

            yield return new WaitForSeconds(2);

            while (PlayerRegistry.Players.Count < 2)
                yield return null;

            PlayerRegistry.Players.Sort((a, b) =>
            a.NetworkObjectId.CompareTo(b.NetworkObjectId));

            SwapPlayers();
        }
    }

    private void SwapPlayers()
    {
        if (!IsServer) return;

        Player playerA = PlayerRegistry.Players[0];
        Player playerB = PlayerRegistry.Players[1];

        if (playerA == null || playerB == null)
            return;

        var ccA = playerA.GetComponent<CharacterController>();
        var ccB = playerB.GetComponent<CharacterController>();

        var playerAScript = playerA.GetComponent<Player>();
        var playerBScript = playerB.GetComponent<Player>();

        //playerAScript.TeleportClientRpc(playerB.transform.position, playerB.transform.rotation, playerB.transform.localScale);
        //playerBScript.TeleportClientRpc(playerA.transform.position, playerA.transform.rotation, playerA.transform.localScale);

        //ClientNetworkTransform cntA = playerA.GetComponent<ClientNetworkTransform>();
        //ClientNetworkTransform cntB = playerB.GetComponent<ClientNetworkTransform>();

        Vector3 posA = playerA.transform.position;
        Vector3 posB = playerB.transform.position;

        Quaternion rotA = playerA.transform.rotation;
        Quaternion rotB = playerB.transform.rotation;

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlaySFX(SFXType.Swop);

        ccA.enabled = false;
        ccB.enabled = false;

        //cntA.Teleport(posB, rotB, playerB.transform.localScale);
        //cntB.Teleport(posA, rotA, playerA.transform.localScale);

        playerA.transform.position = posB;
        playerB.transform.position = posA;

        playerA.transform.rotation = rotB;
        playerB.transform.rotation = rotA;

        ccA.enabled = true;
        ccB.enabled = true;

        Debug.Log("SWAP COMPLETE");
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

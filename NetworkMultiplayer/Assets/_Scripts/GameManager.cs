using System;
using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        //AssignSpawnPositions();
        StartCoroutine(DelayedSpawnSetup());
    }

    private IEnumerator DelayedSpawnSetup()
    {
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2)
            yield return null;

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        while (players.Length < 2)
        {
            yield return null;
            players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        }

        yield return null;
        yield return null;

        AssignSpawnPositions();
    }

    private void AssignSpawnPositions()
    {
        bool hostIsDeck = SessionData.Instance.isHostDeck;

        Player[] players =
            FindObjectsByType<Player>(FindObjectsSortMode.None);

        foreach (Player player in players)
        {
            bool isHostPlayer =
                player.OwnerClientId == NetworkManager.ServerClientId;

            bool playerIsDeck =
                isHostPlayer ? hostIsDeck : !hostIsDeck;

            Transform targetSpawn =
                playerIsDeck ? deckSpawn : cabinSpawn;

            player.SpawnPlayerClientRpc(targetSpawn.position, targetSpawn.rotation, playerIsDeck);

            /*if (playerIsDeck)
            { 
                player.crew.SetActive(true);
                player.captain.SetActive(false);
            }
            else 
            { 
                player.crew.SetActive(false);
                player.captain.SetActive(true);
            }*/

            //StartCoroutine(MovePlayer(player, targetSpawn));

            //ClientNetworkTransform cnt = player.GetComponent<ClientNetworkTransform>();

            /*CharacterController cc =
                player.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            player.transform.SetPositionAndRotation(
                targetSpawn.position,
                targetSpawn.rotation
            );

            //cnt.Teleport(targetSpawn.position, targetSpawn.rotation, Vector3.one);

            //player.transform.position = targetSpawn.position;
            //player.transform.rotation = targetSpawn.rotation;

            if (cc != null)
                cc.enabled = true;*/
        }
    }

    private IEnumerator MovePlayer(Player player, Transform targetSpawn)
    {
        CharacterController cc =
            player.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        yield return null;

        player.transform.SetPositionAndRotation(
            targetSpawn.position,
            targetSpawn.rotation
        );

        yield return null;

        if (cc != null)
            cc.enabled = true;
    }

    /*public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NM_ApprovalCheck;
        NetworkManager.Singleton.StartHost();
    }

    private void NM_ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }*/
}

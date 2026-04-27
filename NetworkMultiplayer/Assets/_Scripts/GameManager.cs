using UnityEngine;
using Unity.Netcode;
using System;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Transform deckSpawn;
    [SerializeField] private Transform cabinSpawn;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

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

            if (playerIsDeck)
            { player.crew.SetActive(true);
                player.captain.SetActive(false);
            }
            else { player.crew.SetActive(false);
                player.captain.SetActive(true);
            }

                CharacterController cc =
                    player.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            player.transform.position = targetSpawn.position;
            player.transform.rotation = targetSpawn.rotation;

            if (cc != null)
                cc.enabled = true;
        }
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

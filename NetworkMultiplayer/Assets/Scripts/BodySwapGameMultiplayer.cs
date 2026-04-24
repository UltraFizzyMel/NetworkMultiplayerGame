using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class BodySwapGameMultiplayer : NetworkBehaviour
{
    

    public static BodySwapGameMultiplayer Instance { get; private set; }

    [SerializeField] private ObjectPickUpListSO objectPickUpListSO;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnObjectPickUp(ObjectPickUpSO objectPickUpSO, IObjectPickUpParent objectPickUpParent)
    {
       SpawnObjectPickUpServerRpc(GetObjectPickUpSOIndex(objectPickUpSO), objectPickUpParent.GetNetworkObject());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnObjectPickUpServerRpc(int objectPickUpSOIndex, NetworkObjectReference objectPickUpParentNetworkObjectReference)
    {
        ObjectPickUpSO objectPickUpSO = GetObjectPickUpSOFromIndex(objectPickUpSOIndex);
        Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab);//spawn the object

        NetworkObject objectPickUpNetworkObject = objectPickUpTransform.GetComponent<NetworkObject>();
        objectPickUpNetworkObject.Spawn(true);
        ObjectPickUp objectPickUp = objectPickUpTransform.GetComponent<ObjectPickUp>();

        objectPickUpParentNetworkObjectReference.TryGet( out NetworkObject objectPickUpParentNetworkObject );
        IObjectPickUpParent objectPickUpParent = objectPickUpParentNetworkObject.GetComponent<IObjectPickUpParent>();
        objectPickUp.SetObjectPickUpParent(objectPickUpParent);
    }

    private  int GetObjectPickUpSOIndex(ObjectPickUpSO objectPickUpSO)
    {
        return objectPickUpListSO.objectPickUpSOList.IndexOf(objectPickUpSO);
    }

    private ObjectPickUpSO GetObjectPickUpSOFromIndex(int objectPickUpSOIndex)
    {
        return objectPickUpListSO.objectPickUpSOList[objectPickUpSOIndex];
    }
}

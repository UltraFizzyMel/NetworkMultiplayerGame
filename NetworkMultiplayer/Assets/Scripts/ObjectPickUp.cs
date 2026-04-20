using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPickUp : NetworkBehaviour
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;

    private IObjectPickUpParent objectPickUpParent;

    private FollowTransform followTransform;


    private void Awake()
    {
        followTransform = GetComponent<FollowTransform>();
        //SpawnObjectPickUp(objectPickUpSO, objectPickUpParent);
    }

    public ObjectPickUpSO GetObjectPickUpSO() { return objectPickUpSO; }


    public void SetObjectPickUpParent(IObjectPickUpParent objectPickUpParent)
    {
        SetObjectPickUpParentServerRpc(objectPickUpParent.GetNetworkObject());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetObjectPickUpParentServerRpc(NetworkObjectReference objectPickUpParentNetworkObjectReference)
    {
        SetObjectPickUpParentClientRpc(objectPickUpParentNetworkObjectReference);
    }

    [ClientRpc]
    private void SetObjectPickUpParentClientRpc(NetworkObjectReference objectPickUpParentNetworkObjectReference)
    {
        objectPickUpParentNetworkObjectReference.TryGet(out NetworkObject objectPickUpNetworkObject);
        IObjectPickUpParent objectPickUpParent = objectPickUpNetworkObject.GetComponent<IObjectPickUpParent>();

        
        if (this.objectPickUpParent != null)// clears the old parent
        {
            this.objectPickUpParent.ClearObjectPickUp();
            Debug.Log("Clearing Parent");
        }
        this.objectPickUpParent = objectPickUpParent; //adds the new parent

        //make sure the new location is empty before placing item there.
        if (objectPickUpParent.HasObjectPickUp())
        {
            Debug.LogError("objectPickUpParent already has a objectPickup");
        }
        objectPickUpParent.SetObjectPickUp(this);

        Debug.Log("Set transform Parent");
        followTransform.SetTargetTransform(objectPickUpParent.GetObjectPickUpTransform());
        //transform.parent = objectPickUpParent.GetObjectPickUpTransform();
        //transform.localPosition = Vector3.zero;
    }

    public IObjectPickUpParent GetObjectPickUpParent() { return objectPickUpParent; }

    public static void SpawnObjectPickUp(ObjectPickUpSO objectPickUpSO, IObjectPickUpParent objectPickUpParent)
    {
        BodySwapGameMultiplayer.Instance.SpawnObjectPickUp(objectPickUpSO, objectPickUpParent);
    }
}

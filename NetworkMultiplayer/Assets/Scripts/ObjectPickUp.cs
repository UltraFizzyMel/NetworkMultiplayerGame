using Unity.Netcode;
using UnityEngine;

public class ObjectPickUp : MonoBehaviour
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;

    private IObjectPickUpParent objectPickUpParent;

    private FollowTransform followTransform;


    private void Awake()
    {
        followTransform = GetComponent<FollowTransform>();
    }

    public ObjectPickUpSO GetObjectPickUpSO() { return objectPickUpSO; }


    public void SetObjectPickUpParent(IObjectPickUpParent objectPickUpParent)
    {
        SetObjectPickUpParentServerRpc(objectPickUpParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetObjectPickUpParentServerRpc(NetworkObjectReference objectPickUpParentNetworkObjectReference)
    {
       
    }

    [ClientRpc]
    private void SetObjectPickUpParentClientRpc(NetworkObjectReference objectPickUpParentNetworkObjectReference)
    {
        objectPickUpParentNetworkObjectReference.TryGet(out NetworkObject objectPickUpNetworkObject);
        IObjectPickUpParent objectPickUpParent = objectPickUpNetworkObject.GetComponent<IObjectPickUpParent>();

        this.objectPickUpParent = objectPickUpParent;
        if (this.objectPickUpParent != null)// clears the old parent
        {
            this.objectPickUpParent.ClearObjectPickUp();
        }
        this.objectPickUpParent = objectPickUpParent; //adds the new parent

        //make sure the new location is empty before placing item there.
        if (objectPickUpParent.HasObjectPickUp())
        {
            Debug.LogError("objectPickUpParent already has a objectPickup");
        }
        objectPickUpParent.SetObjectPickUp(this);

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

using UnityEngine;
using Unity.Netcode;

public class Interactable : NetworkBehaviour, IObjectPickUpParent
{
    [SerializeField] private Transform bucketPlacement;

    [SerializeField] private ObjectPickUp objectPickUp;

    public virtual void Interact(Player player)
    { }

    public Transform GetObjectPickUpTransform()
    {
        return bucketPlacement;
    }

    public void SetObjectPickUp(ObjectPickUp objectPickUp)
    {
        this.objectPickUp = objectPickUp;
    }

    public ObjectPickUp GetObjectPickUp()
    {
        return objectPickUp;
    }


    public void ClearObjectPickUp()
    {
        objectPickUp = null;
    }

    public bool HasObjectPickUp()
    {
        return objectPickUp != null;
    }

    public NetworkObject GetNetworkObject() { return NetworkObject; }
}
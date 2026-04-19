using UnityEngine;

public class BucketZone : MonoBehaviour, IObjectPickUpParent
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;
    [SerializeField] private Transform bucketPlacement;

    [SerializeField] private ObjectPickUp objectPickUp;
    //[SerializeField] private NetworkPlayer networkPlayer;
    //[SerializeField] private BucketController bucketController;
    public void Interact(NetworkPlayer networkPlayer)
    {
        if (objectPickUp == null)
        {
            //Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketHoldPoint); //Instantiate object
            //objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
        }
        else
        {
            //Give the object to the player
            objectPickUp.SetObjectPickUpParent(networkPlayer);

        }
    }

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

    public bool HasObjectPickUp() {
        return objectPickUp != null;
    }
}

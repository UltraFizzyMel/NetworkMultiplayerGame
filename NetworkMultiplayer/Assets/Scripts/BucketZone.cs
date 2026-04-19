using UnityEngine;

public class BucketZone : Interactable, IObjectPickUpParent
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;
    [SerializeField] private Transform bucketPlacement;

    [SerializeField] private ObjectPickUp objectPickUp;
    //[SerializeField] private NetworkPlayer networkPlayer;
    //[SerializeField] private BucketController bucketController;
    public override void Interact(NetworkPlayer networkPlayer)
    {
        if (!HasObjectPickUp())
        {
            //There is no pickup here
            if (networkPlayer.HasObjectPickUp()) {
                //Player has an object in their hands
                networkPlayer.GetObjectPickUp().SetObjectPickUpParent(this);
            }
            else {
                //playernot carrying anything
            }
        }
        else
        {
            //There is a pick-up here
            if (networkPlayer.HasObjectPickUp())
            {
                //player is carrying something
            }
            else
            {
                //player not carrying pickup
                objectPickUp.SetObjectPickUpParent(networkPlayer);
            }
        }
        //Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketHoldPoint); //Instantiate object
        //objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
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

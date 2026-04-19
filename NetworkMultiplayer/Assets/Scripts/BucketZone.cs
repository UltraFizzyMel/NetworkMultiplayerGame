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
        Debug.Log("Interact!!");
        if (!HasObjectPickUp())
        {
            //There is no pickup here
            Debug.Log("No Pick-up");
            if (networkPlayer.HasObjectPickUp()) {
                //Player has an object in their hands
                networkPlayer.GetObjectPickUp().SetObjectPickUpParent(this);
                Debug.Log("Carrying!!");
            }
            else {
                //player not carrying anything
                Debug.Log("Empty!!");
            }
        }
        else
        {
            //There is a pick-up here
            Debug.Log("Pick-up!!");
            if (networkPlayer.HasObjectPickUp())
            {
                //player is carrying something
                Debug.Log("Carrying!!");
            }
            else
            {
                //player not carrying pickup
                objectPickUp.SetObjectPickUpParent(networkPlayer);
                Debug.Log("Empty!!");
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

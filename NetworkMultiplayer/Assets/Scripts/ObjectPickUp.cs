using UnityEngine;

public class ObjectPickUp : MonoBehaviour
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;

    private BucketZone bucketZone;

    public ObjectPickUpSO GetObjectPickUpSO() { return objectPickUpSO; }


    public void SetBucketZone(BucketZone bucketZone)
    { this.bucketZone = bucketZone;
        if(this.bucketZone != null)// clears the old bucket zone
        {
            this.bucketZone.ClearObjectPickUp();
        }
        this.bucketZone = bucketZone; //adds the new bucket zone

        //make sure the new location is empty before placing item there.
        if (bucketZone.HasObjectPickUp())
        {
            Debug.LogError("Zone already has a objectPickup");
        }
        bucketZone.SetObjectPickUp(this);

        transform.parent = bucketZone.GetObjectPickUpTransform();
        transform.localPosition = Vector3.zero;
    }

    public BucketZone GetBucketZone() { return bucketZone; }
}

using UnityEngine;

public class BucketZone : MonoBehaviour
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;
    [SerializeField] private Transform bucketPlacement;

    private ObjectPickUp objectPickUp;
    //[SerializeField] private NetworkPlayer networkPlayer;
    //[SerializeField] private BucketController bucketController;
    public void Interact()
    {
        if (objectPickUp == null)
        {
            Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketPlacement); //Instantiate object
            objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
        }
        else
        {
            Debug.Log(objectPickUp.GetBucketZone());
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

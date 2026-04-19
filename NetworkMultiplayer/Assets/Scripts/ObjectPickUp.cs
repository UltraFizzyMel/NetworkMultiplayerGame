using UnityEngine;

public class ObjectPickUp : MonoBehaviour
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;

    private IObjectPickUpParent objectPickUpParent;

    public ObjectPickUpSO GetObjectPickUpSO() { return objectPickUpSO; }


    public void SetObjectPickUpParent(IObjectPickUpParent objectPickUpParent)
    { this.objectPickUpParent = objectPickUpParent;
        if(this.objectPickUpParent != null)// clears the old bucket zone
        {
            this.objectPickUpParent.ClearObjectPickUp();
        }
        this.objectPickUpParent = objectPickUpParent; //adds the new bucket zone

        //make sure the new location is empty before placing item there.
        if (objectPickUpParent.HasObjectPickUp())
        {
            Debug.LogError("objectPickUpParent already has a objectPickup");
        }
        objectPickUpParent.SetObjectPickUp(this);

        transform.parent = objectPickUpParent.GetObjectPickUpTransform();
        transform.localPosition = Vector3.zero;
    }

    public IObjectPickUpParent GetObjectPickUpParent() { return objectPickUpParent; }
}

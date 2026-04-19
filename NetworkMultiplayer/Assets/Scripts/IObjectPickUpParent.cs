using UnityEngine;

public interface IObjectPickUpParent
{
    public Transform GetObjectPickUpTransform();


    public void SetObjectPickUp(ObjectPickUp objectPickUp);

    public ObjectPickUp GetObjectPickUp();


    public void ClearObjectPickUp();


    public bool HasObjectPickUp();
    

}

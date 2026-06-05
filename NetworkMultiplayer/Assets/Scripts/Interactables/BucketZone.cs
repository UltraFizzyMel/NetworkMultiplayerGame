using System;
using Unity.Netcode;
using Unity.VectorGraphics;
using UnityEngine;

public class BucketZone : Interactable, IObjectPickUpParent
{
    [SerializeField] private ObjectPickUpSO objectPickUpSO;

    public NetworkVariable<bool> isItemBeingPlaced = new(
      false,
      NetworkVariableReadPermission.Everyone,
      NetworkVariableWritePermission.Server);

    //[SerializeField] private Player player;
    //[SerializeField] private BucketController bucketController;




    public override void Interact(Player player)
    {
        if (isItemBeingPlaced.Value == true)
        {
            return;
        }
        ItemPlacementTrueRpc();
        Debug.Log("Interact!!");
        if (!HasObjectPickUp())
        {
            //There is no pickup here
            Debug.Log("No Pick-up");
            if (player.HasObjectPickUp()) {
                //Player has an object in their hands                
                player.GetObjectPickUp().SetObjectPickUpParent(this);

                if (MusicManager.Instance != null)
                {
                    if (player.GetObjectPickUp().TryGetComponent<BucketController>(out BucketController bucketController))
                        MusicManager.Instance.PlaySFX(SFXType.PickupBucket);
                    else if (player.GetObjectPickUp().TryGetComponent<TapeController>(out TapeController tapeController))
                        MusicManager.Instance.PlaySFX(SFXType.PickupDuctTape);
                }                 
                
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
            Debug.Log("There is a Pick-up!!");
            if (player.HasObjectPickUp())
            {
                //player is carrying something
                Debug.Log("Carrying!!");
            }
            else
            {
                //player not carrying pickup
                GetObjectPickUp().SetObjectPickUpParent(player);
                ClearObjectPickUp();
                ClearServerRpc();
                Debug.Log("Empty!!");
            }
        }
        //Transform objectPickUpTransform = Instantiate(objectPickUpSO.prefab, bucketHoldPoint); //Instantiate object
        //objectPickUpTransform.GetComponent<ObjectPickUp>().SetBucketZone(this);
        ItemPlacementFalseRpc();
    }

    public override string GetInteractText(Player player)
    {
        // Empty zone
        if (!HasObjectPickUp())
        {
            if (!player.HasObjectPickUp())
                return "";

            if (player.GetObjectPickUp().TryGetComponent<BucketController>(out _))
                return "Press [E] To\nPlace Bucket";

            if (player.GetObjectPickUp().TryGetComponent<TapeController>(out _))
                return "Press [E] To\nPlace Tape";

            return "";
        }

        // Zone has item
        if (GetObjectPickUp().TryGetComponent<BucketController>(out _))
            return "Press [E] To\nPick Up Bucket";

        if (GetObjectPickUp().TryGetComponent<TapeController>(out _))
            return "Press [E] To\nPick Up Tape";

        return "";
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ClearServerRpc()
    {
       ClearClientRpc();
    }
    [ClientRpc]
    public void ClearClientRpc()
    {
        ClearObjectPickUp();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ItemPlacementTrueRpc()
    {
        isItemBeingPlaced.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ItemPlacementFalseRpc()
    {
        isItemBeingPlaced.Value = false;
    }
}

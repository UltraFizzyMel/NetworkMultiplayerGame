using UnityEngine;
using Unity.Netcode;

public class BucketController : NetworkBehaviour
{
    public NetworkVariable<bool> isFull = new(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    //public bool isFull = false;
    public bool isHoldingBucket;

    public GameObject waterVisual;
    public float bucketCapacity = 0.5f;
    public float waterAmount;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void FillServerRpc()
    {
        isFull.Value = true;
        if (waterVisual != null) waterVisual.SetActive(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EmptyServerRpc()
    {
        isFull.Value = false;
        if (waterVisual != null) waterVisual.SetActive(false);
    }



}

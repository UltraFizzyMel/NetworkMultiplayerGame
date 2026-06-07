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

    private void Start()
    {
        UpdateVisual();

        isFull.OnValueChanged += OnBucketStateChanged;
    }

    private void OnDestroy()
    {
        isFull.OnValueChanged -= OnBucketStateChanged;
    }

    private void OnBucketStateChanged(bool previous, bool current)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (waterVisual != null)
            waterVisual.SetActive(isFull.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void FillServerRpc()
    {
        isFull.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EmptyServerRpc()
    {
        isFull.Value = false;
    }

    /*[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void FillServerRpc()
    {
        FillClientRpc();
    }
    [ClientRpc]
    public void FillClientRpc()
    {
        isFull.Value = true;
        if (waterVisual != null) waterVisual.SetActive(true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EmptyServerRpc()
    {
        EmptyClientRpc();
    }

    [ClientRpc]
    public void EmptyClientRpc()
    {
        isFull.Value = false;
        if (waterVisual != null) waterVisual.SetActive(false);
    }*/

}

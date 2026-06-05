using Unity.Netcode;
using UnityEngine;

public class InteractChecker : MonoBehaviour
{
    public NetworkVariable<bool> isItemBeingPlaced = new(
      false,
      NetworkVariableReadPermission.Everyone,
      NetworkVariableWritePermission.Server);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

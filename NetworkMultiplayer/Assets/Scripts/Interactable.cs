using UnityEngine;
using Unity.Netcode;

public class Interactable : NetworkBehaviour
{

    public virtual void Interact(NetworkPlayer networkPlayer)
    { }

}
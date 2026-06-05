using System;
using Unity.Netcode;
using UnityEngine;

public class Radio : Interactable
{
    public override void Interact(Player player)
    {

       
    }

    public override string GetInteractText(Player player)
    {
                return interactText;  
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// Title: Raycast Interactions:Let's make a first person game in Unity
// Author: Natty GameDev
// Date: 14 August 2024
// Code Version: 1.0
// Availability: www.youtube.com/watch?v=gPPGnpV1Y1c

//Uses Template Method Pattern
public abstract class Interactable : MonoBehaviour
{
    //message displayed to the player when looking at an interactable.
    public string promptMessage;

    //public Image UIButtonPrompt;

    //This function will be called from the player script
    public void BaseInteract()
    {
        Interact();
    }

    protected virtual void Interact()
    {
        //This is a template function to be overwritten by subclasses.

    }
}
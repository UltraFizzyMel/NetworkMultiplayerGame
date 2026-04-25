using System;
using Unity.Netcode;
using UnityEngine;

public class BoatWinLoseController : MonoBehaviour {

    [SerializeField] BoatLeakManager cabinLeakManager;
    [SerializeField] BoatLeakManager deckLeakManager;
    [SerializeField] BoatMovement boatMovement;

    private void Update()
    {
        if(cabinLeakManager.CheckLossCondition() || deckLeakManager.CheckLossCondition())
        { Debug.Log("Game Lost"); }
        if(boatMovement.CheckWinCondition())
        { Debug.Log("Game Won"); }
    }

}

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatWinLoseController : NetworkBehaviour {

    [SerializeField] BoatLeakManager cabinLeakManager;
    [SerializeField] BoatLeakManager deckLeakManager;
    [SerializeField] BoatMovement boatMovement;

    private void Update()
    {
        if (!IsServer) { return; }
        if(cabinLeakManager.CheckLossCondition() || deckLeakManager.CheckLossCondition())
        {
            Debug.Log("Game Lost");
            NetworkManager.SceneManager.LoadScene("LostGame", LoadSceneMode.Single);
             
        }

        if(boatMovement.CheckWinCondition())
        {
            Debug.Log("Game Won");
            NetworkManager.SceneManager.LoadScene("WonGame", LoadSceneMode.Single);
        }
    }

}

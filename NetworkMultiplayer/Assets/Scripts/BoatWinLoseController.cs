using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatWinLoseController : NetworkBehaviour {

    [SerializeField] BoatLeakManager cabinLeakManager;
    [SerializeField] BoatLeakManager deckLeakManager;
    [SerializeField] BoatMovement boatMovement;

    public static BoatWinLoseController Instance { get; private set; }

    private bool _isGameOver;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (!IsServer || _isGameOver)  return;
        if(cabinLeakManager.CheckLossCondition() || deckLeakManager.CheckLossCondition())
        {
            _isGameOver = true;
            Debug.Log("[WinLose] Game Lost");
            NetworkManager.SceneManager.LoadScene("LostGame", LoadSceneMode.Single);
        }

        /*if(boatMovement.CheckWinCondition())
        {
            Debug.Log("Game Won");
            NetworkManager.SceneManager.LoadScene("WonGame", LoadSceneMode.Single);
        }*/
    }

    public void WinGame()
    {
        if (!IsServer || _isGameOver) return;

        _isGameOver = true;
        Debug.Log("[WinLose] Game Won");
        NetworkManager.SceneManager.LoadScene("WonGame", LoadSceneMode.Single);
    }
}

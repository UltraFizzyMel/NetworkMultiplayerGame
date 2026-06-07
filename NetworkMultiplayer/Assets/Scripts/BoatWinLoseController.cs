using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoatWinLoseController : NetworkBehaviour {

    [SerializeField] BoatLeakManager cabinLeakManager;
    [SerializeField] BoatLeakManager deckLeakManager;
    [SerializeField] BoatMovement boatMovement;

    public static BoatWinLoseController Instance { get; private set; }

    //public static LossType CurrentLossType = LossType.None;
    private bool _isGameOver;

    [SerializeField] private AudioClip loseMusic;
    [SerializeField] private AudioClip wonMusic;

    private void Awake() => Instance = this;

    private void Update()
    {
        if (!IsServer || _isGameOver)  return;
        if(cabinLeakManager.CheckLossCondition() || deckLeakManager.CheckLossCondition())
        {
            //_isGameOver = true;
            //Debug.Log("[WinLose] Game Lost");
            //NetworkManager.SceneManager.LoadScene("LostGame", LoadSceneMode.Single);

            SessionData.Instance.currentLossType = LossType.Sank;
            LoseGame();
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

        //Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;

        _isGameOver = true;
        Debug.Log("[WinLose] Game Won");

        UnlockCursorClientRpc();

        MusicManager.Instance.CrossfadeToNewSong(wonMusic, "Won Music", 0.05f, 0.05f);
        NetworkManager.SceneManager.LoadScene("WonGame", LoadSceneMode.Single);
    }

    // Called by FogZoneManager (death zone timeout) and water loss condition.
    public void LoseGame()
    {
        if (!IsServer || _isGameOver) return;

        //Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;

        _isGameOver = true;
        Debug.Log("[WinLose] Lost");

        SetLossTypeClientRpc((int)SessionData.Instance.currentLossType);
        UnlockCursorClientRpc();

        MusicManager.Instance.CrossfadeToNewSong(loseMusic, "Lose Music", 0.05f, 0.7f);
        NetworkManager.SceneManager.LoadScene("LostGame", LoadSceneMode.Single);
    }

    [ClientRpc]
    private void UnlockCursorClientRpc()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    [ClientRpc]
    private void SetLossTypeClientRpc(int lossType)
    {
        if (SessionData.Instance == null)
            return;

        SessionData.Instance.currentLossType = (LossType)lossType;
    }
}

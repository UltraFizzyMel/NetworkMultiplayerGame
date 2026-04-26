using UnityEngine;

public class JoinLobbyButton : MonoBehaviour
{
    public bool needPassword;
    public string lobbyID;

    public void JoinLobbyButtonPressed()
    {
        LobbyManager.Instance.JoinLobby(lobbyID, needPassword);
    }
}

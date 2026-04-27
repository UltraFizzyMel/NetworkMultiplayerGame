using UnityEngine;

public class JoinLobbyButton : MonoBehaviour
{
    public bool needPassword;
    public string lobbyID;

    public void JoinLobbyButtonPressed()
    {
        Debug.Log($"=== JOIN BUTTON PRESSED ===");
        Debug.Log($"LobbyID: '{lobbyID}'");
        Debug.Log($"NeedPassword: {needPassword}");
        Debug.Log($"LobbyManager Instance exists: {LobbyManager.Instance != null}");

        if (string.IsNullOrWhiteSpace(lobbyID))
        {
            Debug.LogError("LobbyID is null or empty!");
            return;
        }

        LobbyManager.Instance.JoinLobby(lobbyID, needPassword);
    }
}

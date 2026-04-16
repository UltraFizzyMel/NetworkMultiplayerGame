using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{ 
    [SerializeField]
    private TextMeshProUGUI promptText;

    [SerializeField]
    public GameObject HoldingUI;


    [Header("INTERACT UI Images")]
    [SerializeField]
    public GameObject UIPrompt;


    // Start is called before the first frame update
    void Start()
    { 
        HoldingUI.SetActive(false);
        UIPrompt.SetActive(false);    
    }


    public void UpdateText(string promptMessage)
    {
        promptText.text = promptMessage;
    }


    //Called when a player Picks up an object
    public void PickUpObjectUI()
    {
        HoldingUI.SetActive(true);
    }

    //Called when a player drops an object
    public void PutDownObjectUI()
    {
        HoldingUI.SetActive(false);
    }

}
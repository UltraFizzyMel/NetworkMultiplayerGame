using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InteractUI : NetworkBehaviour
{

    [SerializeField]
    private TextMeshProUGUI promptText;

    //public GameObject UIPrompt;

    public static InteractUI instance;
    public Player player;


    void Awake()
    {
        player = GetComponent<Player>();
       
       

        if (instance == null)
        { instance = this; }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        //if (IsOwner) { promptText = GameObject.Find("InteractText").GetComponent<TextMeshProUGUI>(); }
        //UIPrompt.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;  
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner) { promptText = GameObject.Find("InteractText").GetComponent<TextMeshProUGUI>(); }
        
    }

    public void Update()
    {
        UpdateText(string.Empty);
       
        if (Physics.Raycast(player.cameraPivot.position, player.cameraPivot.forward, out RaycastHit hitInfo, player.interactionDistance)) //code only runs if raycast hits something
        {

            //Checks to see if gameobject has an interactable component
            if (hitInfo.collider.GetComponent<Interactable>() != null)
            {
                //Stores interactable in a variable
                Interactable interactable = hitInfo.collider.GetComponent<Interactable>();

                //updates onscreen text to match the prompt message of the interactable
                UpdateText(interactable.interactText);                         
            }
        }
        
            
    }


    public void UpdateText(string promptMessage)
    {
        if (promptText == null)
        { return; }
            promptText.text = promptMessage;
    }

}


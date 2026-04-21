
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.VisualScripting;
using System;


public class NetworkPlayer : NetworkBehaviour, IObjectPickUpParent
{ 
    [Header("PlayerComponents")]
    [SerializeField] private Transform cameraPivot;     //empty child at head height
    [SerializeField] private Camera playerCamera;   //child camera

    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxPitch = 80f;

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction interact;
    private CharacterController cc;

    private float pitch;    //Current up/down camera rotation

    [Header("Bucket Settings")]
    public BucketController bucketController;
    public BoatLeakManager boatLeakManagerDeck;
    public BoatLeakManager boatLeakManagerCabin;
    public float interactionDistance = 5f;
    public bool hasBucket;
    public event EventHandler OnInteractAction;

    [Header("Interface settings")]
    [SerializeField] private Transform bucketHoldPoint;
    [SerializeField] private ObjectPickUp objectPickUp;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        //boatLeakManagerCabin = GameObject.Find("CabinWater").GetComponent<BoatLeakManager>();
        //boatLeakManagerDeck = GameObject.Find("DeckWater").GetComponent<BoatLeakManager>();
       // bucketController = GameObject.Find("TempBucket").GetComponent<BucketController>();//change when changing name of bucket gameobject

        if (!IsOwner)
        {
            //Only the owning player should have an active camera and input
            if (playerCamera) playerCamera.enabled = false;
            if (pi) pi.enabled = false;
            enabled = false;
            return;
        }

        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        jumpAction = pi.actions["Jump"];
        interact = pi.actions["Interact"];
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        interact.Enable();

        if (playerCamera) playerCamera.enabled = true;

        interact.performed += Interact_performed;
    }

    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
        HandleInteractions();
    }

    private void Update()
    {
        //Move (X/Z)
        Vector2 m = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * m.x + transform.forward * m.y;
        cc.Move(move * moveSpeed * Time.deltaTime);

        //Look
        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, look.x, 0f); // yaw = rotate the whole character left/right around y axis

        pitch -= look.y;// pitch = rotate the pamera pivot up/down(invert look.y by subtracting)
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);//clamp camera so player doesnt turn over
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f); // Apply pitch to the camera pivot only(keeps body upright)

        //TryScoop();
        //HandleInteractions();
    }

    private void HandleInteractions()
    {
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit raycastHit, interactionDistance))
        {
            if(raycastHit.transform.TryGetComponent(out Interactable interactable))
            {
                //Has interactable              
                    interactable.Interact(this);               
            }
            else
            {

            }
        }
        
        
    }
   /* void TryScoop()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out hit, interactionDistance))
        {
            if (!bucketController.isFull)
            {
                if (hit.collider.name == "DeckWater")
                {
                    boatLeakManagerDeck.bucketUsed = true;
                    
                }
                else if (hit.collider.name == "CabinWater")
                {
                    boatLeakManagerCabin.bucketUsed = true;
                }
                bucketController.Fill();
            }
            else
            {
                if (hit.collider.CompareTag("ShipDeck"))
                {
                    boatLeakManagerDeck.bucketRebound = true;
                    bucketController.Empty();
                }
                else if (hit.collider.CompareTag("ShipCabin"))
                {
                    boatLeakManagerCabin.bucketRebound = true;
                    bucketController.Empty();
                }
                else if (hit.collider.CompareTag("OffShip"))
                {
                   bucketController.Empty();
                }
                
            }
           
        }
    }*/

    public Transform GetObjectPickUpTransform()
    {
        return bucketHoldPoint;
    }

    public void SetObjectPickUp(ObjectPickUp objectPickUp)
    {
        this.objectPickUp = objectPickUp;
    }

    public ObjectPickUp GetObjectPickUp()
    {
        return objectPickUp;
    }

    public void ClearObjectPickUp()
    {
        objectPickUp = null;
    }

    public bool HasObjectPickUp()
    {
        return objectPickUp != null;
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}

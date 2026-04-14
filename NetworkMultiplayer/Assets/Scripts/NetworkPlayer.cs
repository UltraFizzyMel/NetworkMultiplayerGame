
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;


public class NetworkPlayer : NetworkBehaviour
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
    private CharacterController cc;

    private float pitch;    //Current up/down camera rotation

    [Header("Bucket Settings")]
    public BucketController bucketController;
    public BoatLeakManager boatLeakManagerDeck;
    public BoatLeakManager boatLeakManagerCabin;
    public float interactionDistance = 3f;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

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
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();

        if (playerCamera) playerCamera.enabled = true;
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

        TryScoop();
    }


    void TryScoop()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out hit, interactionDistance))
        {
            if (!bucketController.isFull)
            {
                if (hit.collider.name == "DeckWater")
                {
                    boatLeakManagerDeck.currentWaterLevel -= bucketController.bucketCapacity;
                    if (boatLeakManagerDeck.currentWaterLevel < 0)
                        boatLeakManagerDeck.currentWaterLevel = 0;
                }
                else if (hit.collider.name == "CabinWater")
                {
                    boatLeakManagerCabin.currentWaterLevel -= bucketController.bucketCapacity;
                    if (boatLeakManagerCabin.currentWaterLevel < 0)
                        boatLeakManagerCabin.currentWaterLevel = 0;
                }
                bucketController.Fill();
            }
            else
            {
                if (hit.collider.CompareTag("ShipDeck"))
                {
                    boatLeakManagerDeck.currentWaterLevel += bucketController.bucketCapacity;
                    if (boatLeakManagerDeck.currentWaterLevel > boatLeakManagerDeck.maxWaterLevel)
                        boatLeakManagerDeck.currentWaterLevel = boatLeakManagerDeck.maxWaterLevel;
                }
                else if (hit.collider.CompareTag("ShipCabin"))
                {
                    boatLeakManagerCabin.currentWaterLevel += bucketController.bucketCapacity;
                    if (boatLeakManagerCabin.currentWaterLevel > boatLeakManagerCabin.maxWaterLevel) 
                        boatLeakManagerCabin.currentWaterLevel = boatLeakManagerCabin.maxWaterLevel;
                }
                else if (hit.collider.CompareTag("OffBoat"))
                {

                }
                bucketController.Empty();
            }
           
        }
    }
}

using System;
using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour, IObjectPickUpParent
{
    [Header("Player Components")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float gravity = -9.81f;
    private Vector3 velocity;

    private PlayerInput pi;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction swapAction;
    private InputAction interact;
    private InputAction interactAlternate;
    private ClientNetworkTransform cnt;
    private CharacterController cc;
    public GameObject captain;
    public GameObject crew;

    private float pitch;

    [Header("Bucket Settings")]
    public BucketController bucketController;
    public BoatLeakManager boatLeakManagerDeck;
    public BoatLeakManager boatLeakManagerCabin;
    public float interactionDistance = 5f;
    public bool hasBucket;
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public Interactable lastInteractable;

    [Header("Interface settings")]
    [SerializeField] private Transform bucketHoldPoint;
    [SerializeField] private ObjectPickUp objectPickUp;

    [Header("Global Volume Settings")]
    [SerializeField] private Volume globalVolume;
    private LensDistortion lens;
    [SerializeField] float lensChangeValue = -0.45f;
    private Vignette vignette;
    [SerializeField] float vignetteChangeValue = 0.55f;
    private ChromaticAberration chromatic;
    [SerializeField] float chromaticChangeValue = 1f;
    [SerializeField] private float teleportEffectDuration = 0.8f;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        cnt = GetComponent<ClientNetworkTransform>();
        pi = GetComponent<PlayerInput>();

        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        PlayerRegistry.Register(this);

        if (!IsOwner)
        {
            if (playerCamera)
                playerCamera.enabled = false;
            if (pi)
                pi.enabled = false;
            if (globalVolume)
                globalVolume.enabled = false;

            //enabled = false;
            return;
        }

        /*if (!IsServer)
        {
            cc.enabled = false;
        }*/

        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        swapAction = pi.actions["Swap"];
        interact = pi.actions["Interact"];
        interactAlternate = pi.actions["InteractAlternate"];
        moveAction.Enable();
        lookAction.Enable();
        swapAction.Enable();
        interact.Enable();
        interactAlternate.Enable();

        if (playerCamera)
            playerCamera.enabled = true; //start off

        if (pi)
            pi.enabled = true;

        //controllingClientId.OnValueChanged += OnControlChanged;

        //OnControlChanged(0, controllingClientId.Value);
        interact.performed += Interact_performed;
        interact.canceled += Interact_canceled;
        interactAlternate.performed += InteractAlternate_performed;              

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out lens);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out chromatic);

            Debug.Log($"Lens: {lens}");
            Debug.Log($"Vignette: {vignette}");
            Debug.Log($"Chromatic: {chromatic}");
        }
    }

    private void InteractAlternate_performed(InputAction.CallbackContext obj)
    {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
        HandleInteractionsAlternate();
    }

    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
        HandleInteractions();
    }
    private void Interact_canceled(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
        HandleCancel();
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned || cc.enabled == false)
            return;

        //if (NetworkManager.Singleton.LocalClientId != controllingClientId.Value)
        //  return;

        //if (controllingClientId.Value == ulong.MaxValue)
        //  return;

        Vector2 m = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * m.x + transform.forward * m.y;
        if (cc.enabled)
            cc.Move(move * moveSpeed * Time.deltaTime);

        //Move(m);

        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);
        //Look(look.x);

        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        ApplyGravity();

        if (swapAction.WasPressedThisFrame())
        {
            RequestSwapServerRpc();
        }
    }

    [ClientRpc]
    public void SpawnPlayerClientRpc(Vector3 pos, Quaternion rot, bool isDeck)
    {
        StartCoroutine(ApplySpawn(pos, rot, isDeck));
    }

    private IEnumerator ApplySpawn(Vector3 pos, Quaternion rot, bool isDeck)
    {
        yield return null;
        yield return null;

        if (cc != null)
            cc.enabled = false;

        transform.position = pos;
        transform.rotation = rot;

        if (cnt != null && IsOwner)
            cnt.Teleport(pos, rot, transform.localScale);

        crew.SetActive(isDeck);
        captain.SetActive(!isDeck);

        if (cc != null)
            cc.enabled = true;
    }

    [ServerRpc]
    private void RequestSwapServerRpc()
    {
        SwapManager.Instance.TrySwap();
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 pos, Quaternion rot)
    {
        if (!IsOwner)
            return;

        StartCoroutine(TeleportSequence(pos, rot));

        /*if (cc != null)
            cc.enabled = false;
        
        if (cnt)
        {
            cnt.Teleport(pos, rot, Vector3.one);
        }

        transform.SetPositionAndRotation(pos, rot);

        if (cc != null)
            cc.enabled = true;*/
    }

    private IEnumerator TeleportSequence(Vector3 pos, Quaternion rot)
    {
        float duration = teleportEffectDuration;
        float halfDuration = duration * 0.5f;

        float startLens = 0f;
        float peakLens = lensChangeValue;

        float startVignette = 0.4f;
        float peakVignette = vignetteChangeValue;

        float startChromatic = 0f;
        float peakChromatic = chromaticChangeValue;

        float timer = 0f;

        // PHASE 1
        // Ramp UP toward teleport

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t = timer / halfDuration;

            // Smooth easing
            float eased = Mathf.SmoothStep(0f, 1f, t);

            lens.intensity.value =
                Mathf.Lerp(startLens, peakLens, eased);

            vignette.intensity.value =
                Mathf.Lerp(startVignette, peakVignette, eased);

            chromatic.intensity.value =
                Mathf.Lerp(startChromatic, peakChromatic, eased);

            yield return null;
        }

        Debug.Log("Lens value: " + lens.intensity.value);
        Debug.Log("Vignette value: " + vignette.intensity.value);
        Debug.Log("Chromatic value: " + chromatic.intensity.value);

        // TELEPORT AT PEAK

        if (cc != null)
            cc.enabled = false;

        Vector3 safePos = pos + Vector3.up * 0.2f;

        transform.SetPositionAndRotation(safePos, rot);

        if (cnt != null)
        {
            cnt.Teleport(safePos, rot, transform.localScale);
        }

        if (cc != null)
            cc.enabled = true;

        // PHASE 2
        // Ramp DOWN after teleport

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t = timer / halfDuration;

            float eased = Mathf.SmoothStep(0f, 1f, t);

            lens.intensity.value =
                Mathf.Lerp(peakLens, startLens, eased);

            vignette.intensity.value =
                Mathf.Lerp(peakVignette, startVignette, eased);

            chromatic.intensity.value =
                Mathf.Lerp(peakChromatic, startChromatic, eased);

            yield return null;
        }

        // FINAL RESET

        lens.intensity.value = startLens;
        vignette.intensity.value = startVignette;
        chromatic.intensity.value = startChromatic;
    }

    /*private void LateUpdate()
    {
        if (!IsOwner || !IsSpawned)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        pitch -= look.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }*/

    private void HandleInteractions()
    {
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit raycastHit, interactionDistance))
        {
            if (raycastHit.transform.TryGetComponent(out Interactable interactable))
            {
                //Has interactable              
                interactable.Interact(this);
                lastInteractable = interactable;
            }
            else
            {

            }
        }
    }

    private void HandleCancel()
    {
        if (lastInteractable != null)
        { lastInteractable.Cancel(this); }
        /*if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit raycastHit, interactionDistance))
        {
            if (raycastHit.transform.TryGetComponent(out Interactable interactable))
            {
                //Has interactable              
                interactable.Cancel(this);
            }
            else
            {

            }
        }*/
    }

    private void HandleInteractionsAlternate()
    {
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit raycastHit, interactionDistance))
        {
            if (raycastHit.transform.TryGetComponent(out Interactable interactable))
            {
                //Has interactable              
                interactable.InteractAlternate(this);
            }
            else
            {

            }
        }
    }

     private void ApplyGravity()
     {
       if (cc.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
     }

    private void Move(Vector2 input)
    {
        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        move.y = 0f;
        move.Normalize();

        cc.Move(moveSpeed * Time.deltaTime * move);
    }

    private void Look(float yawInput)
    {
        transform.Rotate(0f, yawInput, 0f);
    }

    /*[ServerRpc]
    private void ApplyGravityServerRpc()
    {
        ApplyGravity();
    }*/

    public override void OnNetworkDespawn()
    {
        PlayerRegistry.Unregister(this);
    }


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

using System;
using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//using UnityEngine.UIElements;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class Player : NetworkBehaviour, IObjectPickUpParent
{
    [Header("Player Components")]
    [SerializeField] public Transform cameraPivot;
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

    [Header("Steering")]
    [SerializeField] private float steeringLookLimit = 35f;
    [SerializeField] private float steeringLookSensitivity = 1f;

    private bool isSteering;
    private bool canMove = true;

    private SteeringWheel currentWheel;

    private float steeringYaw;
    private float steeringPitch;

    private float _lastSentSteering = float.NaN;

    [Header("Global Volume Settings")]
    [SerializeField] private Volume globalVolume;
    private LensDistortion lens;
    [SerializeField] private float lensBaseValue = 0f;
    [SerializeField] float lensChangeValue = -0.45f;
    private Vignette vignette;
    [SerializeField] private float vignetteBaseValue = 0.4f;
    [SerializeField] float vignetteChangeValue = 0.55f;
    private ChromaticAberration chromatic;
    [SerializeField] private float chromaticBaseValue = 0f;
    [SerializeField] float chromaticChangeValue = 1f;
    [SerializeField] private float teleportEffectDuration = 0.8f;

    [Header("UI Settings")]
    [SerializeField] private GameObject TentacleUI;
    [SerializeField]private float maxTransparency = 250f;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"=== PLAYER SPAWNED === OwnerClientId: {OwnerClientId}, IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}, NetworkObjectId: {NetworkObjectId}");
        Debug.Log($"Player GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");

        cc = GetComponent<CharacterController>();
        cnt = GetComponent<ClientNetworkTransform>();
        pi = GetComponent<PlayerInput>();

        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        PlayerRegistry.Register(this);

        if (IsServer && !IsOwner)
        {
            Debug.Log($"[Player] Server-owned copy of client {OwnerClientId}'s player - disabling components");

            // Disable ALL components that could cause issues
            if (playerCamera) playerCamera.enabled = false;
            if (pi) pi.enabled = false;
            if (globalVolume) globalVolume.enabled = false;

            // Disable this script to prevent Update from running
            //enabled = false;

            // Still register but don't initialize further
            PlayerRegistry.Register(this);
            return;
        }

        /*if (!IsServer)
        {
            cc.enabled = false;
        }*/

        if (!IsOwner)
        {
            if (playerCamera)
                playerCamera.enabled = false;
            if (pi)
                pi.enabled = false;
            if (globalVolume)
                globalVolume.enabled = false;

            return;
        }

        moveAction = pi.actions["Move"];
        lookAction = pi.actions["Look"];
        interact = pi.actions["Interact"];
        interactAlternate = pi.actions["InteractAlternate"];
        moveAction.Enable();
        lookAction.Enable();
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
        TentacleUI = GameObject.Find("TentacleUI");
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
        if (!IsOwner || !IsSpawned)// || cc.enabled == false)
            return;

        if (isSteering)
        {
            HandleSteeringMode();
            return;
        }

        if (!canMove)
            return;

        NormalMovementUpdate();

        /*ApplyGravity();

        Vector2 m = moveAction.ReadValue<Vector2>();
        if (isSteering)
        {
            currentWheel.HandleSteeringInput(m.x);
            return;
        }

        Vector3 move = transform.right * m.x + transform.forward * m.y;
        if (cc.enabled)
            cc.Move(move * moveSpeed * Time.deltaTime);

        Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
        transform.Rotate(0f, look.x, 0f);

        pitch -= look.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f); */       
    }

    private void NormalMovementUpdate()
    {
        ApplyGravity();

        Vector2 m = moveAction.ReadValue<Vector2>();

        Vector3 move =
            transform.right * m.x +
            transform.forward * m.y;

        cc.Move(move * moveSpeed * Time.deltaTime);

        Vector2 look =
            lookAction.ReadValue<Vector2>() *
            lookSensitivity;

        transform.Rotate(0f, look.x, 0f);

        pitch -= look.y;

        pitch = Mathf.Clamp(
            pitch,
            -maxPitch,
            maxPitch
        );

        cameraPivot.localEulerAngles =
            new Vector3(pitch, 0f, 0f);
    }

    private void HandleSteeringMode()
    {
        transform.position = currentWheel.steeringPosition.position;

        transform.rotation = Quaternion.Euler(0f, currentWheel.steeringPosition.eulerAngles.y, 0f);

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitSteering();
            return;
        }

        /*Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // ONLY A/D
        currentWheel.HandleSteeringInput(moveInput.x);*/

        float steeringInput = moveAction.ReadValue<Vector2>().x;

        // Only fire the ServerRpc when input actually changes
        if (!Mathf.Approximately(steeringInput, _lastSentSteering))
        {
            _lastSentSteering = steeringInput;
            currentWheel.HandleSteeringInput(steeringInput);
        }

        // LIMITED CAMERA LOOK
        Vector2 look = lookAction.ReadValue<Vector2>() * steeringLookSensitivity;

        // HORIZONTAL LOOK
        steeringYaw += look.x;

        steeringYaw = Mathf.Clamp(steeringYaw, -steeringLookLimit, steeringLookLimit);

        // VERTICAL LOOK
        steeringPitch -= look.y;

        steeringPitch = Mathf.Clamp(steeringPitch, -15f, 15f);

        // ROTATE CAMERA ONLY
        cameraPivot.localRotation = Quaternion.Euler(steeringPitch, steeringYaw, 0f);
    }

    [ClientRpc]
    public void SpawnPlayerClientRpc(Vector3 position, Quaternion rotation, bool isDeck)
    {
        ApplyRoleVisuals(isDeck);

        if (IsOwner)
            StartCoroutine(PhysicsSafeSpawn(position, rotation));
        else
            StartCoroutine(HideOverlayWhenOwnerReady());
    }

    private IEnumerator PhysicsSafeSpawn(Vector3 position, Quaternion rotation)
    {
        if (cc != null) cc.enabled = false;

        Vector3 safePosition = position + Vector3.up * 0.05f;

        if (Physics.Raycast(
                position + Vector3.up * 2f,
                Vector3.down,
                out RaycastHit hit,
                10f,
                ~LayerMask.GetMask("Player")))
        {
            safePosition = hit.point + Vector3.up * (cc != null ? cc.skinWidth + 0.01f : 0.05f);
        }

        transform.SetPositionAndRotation(safePosition, rotation);
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        const float groundedTimeout = 5f;
        float elapsed = 0f;

        while (cc != null && !cc.isGrounded && elapsed < groundedTimeout)
        {
            cc.Move(Vector3.down * (9.81f * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (cnt != null)
            cnt.Teleport(safePosition, rotation, transform.localScale);

        // Owner is grounded — hide the overlay on this client.
        HideLoadingOverlay();
    }

    // Non-owner waits for the owner's PhysicsSafeSpawn to finish (detected by the overlay being hidden), then hides its own copy.
    private IEnumerator HideOverlayWhenOwnerReady()
    {
        var overlay = GameManager.Instance?.LoadingOverlay;
        if (overlay == null) { HideLoadingOverlay(); yield break; }

        const float timeout = 10f;
        float elapsed = 0f;

        while (overlay.activeSelf && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideLoadingOverlay();
    }

    private static void HideLoadingOverlay()
    {
        if (GameManager.Instance != null && GameManager.Instance.LoadingOverlay != null)
            GameManager.Instance.LoadingOverlay.SetActive(false);
    }

    [ClientRpc]
    public void ApplyRoleVisualsClientRpc(bool isDeck)
    {
        ApplyRoleVisuals(isDeck);
    }

    private void ApplyRoleVisuals(bool isDeck)
    {
        if (crew != null)
            crew.SetActive(isDeck);

        if (captain != null)
            captain.SetActive(!isDeck);
    }

    // Called by SwapManager's ClientRpc on the owner client only.
    public void StartSwapWarning(float duration)
    {
        if (!IsOwner) return;
        StopCoroutine(nameof(SwapWarningSequence)); // Prevent stacking
        StartCoroutine(SwapWarningSequence(duration));
    }

    private IEnumerator SwapWarningSequence(float totalDuration)
    {
        // ── Baseline values (match resting / TeleportSequence start values) ─
        const float baseVignette = 0.4f;
        const float peakVignette = 0.65f; // Noticeably dark but not blinding

        const float baseChromatic = 0f;
        const float peakChromatic = 0.55f;

        const float baseLens = 0f;
        const float peakLens = -0.6f;  // Slight squeeze

        const float baseTransparency = 0f;

        // ── How fast the pulse heartbeat beats (starts slow, quickens) ───────────
        // Pulse frequency ramps from 0.5 Hz to 3 Hz over the warning window.
        float minFreq = 0.5f;
        float maxFreq = 3.0f;

        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / totalDuration); // 0 → 1 over the window
            float eased = Mathf.SmoothStep(0f, 1f, t);
            //float extraEased = Mathf.SmoothStep(0f, 50f, t);

            // Steady ramp ────────────────────────────────────────────────────────
            float vignetteBase = Mathf.Lerp(baseVignette, peakVignette, eased);
            float chromaticBase = Mathf.Lerp(baseChromatic, peakChromatic, eased);
            float lensBase = Mathf.Lerp(baseLens, peakLens, eased);
            float transparencyvalue = Mathf.Lerp(baseTransparency, maxTransparency, t);

            // Heartbeat pulse on top of the ramp ─────────────────────────────────
            // Frequency increases as t approaches 1
            float freq = Mathf.Lerp(minFreq, maxFreq, t);
            float pulse = Mathf.Abs(Mathf.Sin(timer * freq * Mathf.PI)); // 0 → 1 → 0 …
                                                                         // Scale pulse magnitude up as the swap gets closer
            float pulseAmt = Mathf.Lerp(0.04f, 0.12f, t) * pulse;

            

            vignette.intensity.value = Mathf.Clamp01(vignetteBase + pulseAmt);
            chromatic.intensity.value = Mathf.Clamp01(chromaticBase);
            lens.intensity.value = lensBase;
            

            RawImage tentacleImage = TentacleUI.GetComponent<RawImage>();
            Color colorVar = tentacleImage.color;
            colorVar.a = Mathf.Clamp01(transparencyvalue);
            tentacleImage.color = colorVar;

            yield return null;
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 pos, Quaternion rot)
    {
        if (!IsOwner)
            return;

        if (isSteering) ExitSteering();

        StartCoroutine(TeleportSequence(pos, rot));
    }

    private IEnumerator TeleportSequence(Vector3 pos, Quaternion rot)
    {
        float duration = teleportEffectDuration;
        float halfDuration = duration * 0.5f;

        float peakLens = lensChangeValue;
        float peakVignette = vignetteChangeValue;
        float peakChromatic = chromaticChangeValue;

        float timer = 0f;

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
            float extraEased = Mathf.SmoothStep(0f, 3f, t);

            lens.intensity.value =
                Mathf.Lerp(peakLens, lensBaseValue, eased);

            vignette.intensity.value =
                Mathf.Lerp(peakVignette, vignetteBaseValue, eased);

            chromatic.intensity.value =
                Mathf.Lerp(peakChromatic, chromaticBaseValue, eased);

            RawImage tentacleImage = TentacleUI.GetComponent<RawImage>();
            Color colorVar = tentacleImage.color;
            colorVar.a = Mathf.Lerp(maxTransparency, 0f, extraEased); 
            tentacleImage.color = colorVar;

            yield return null;
        }

        // FINAL RESET

        lens.intensity.value = lensBaseValue;
        vignette.intensity.value = vignetteBaseValue;
        chromatic.intensity.value = chromaticBaseValue;
    }

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

    public bool IsSteering()
    {
        return isSteering;
    }

    public void EnterSteering(SteeringWheel wheel)
    {
        if (isSteering)
            return;

        isSteering = true;

        currentWheel = wheel;

        canMove = false;

        velocity = Vector3.zero;

        //cc.enabled = false;

        /*transform.SetPositionAndRotation(
            wheel.steeringPosition.position,
            wheel.steeringPosition.rotation
        );*/

        transform.position = wheel.steeringPosition.position;

        transform.rotation =
            Quaternion.Euler(
                0f,
                wheel.steeringPosition.eulerAngles.y,
                0f
            );

        steeringYaw = 0f;
        steeringPitch = 0f;
    }

    public void ExitSteering()
    {
        if (!isSteering)
            return;

        isSteering = false;

        currentWheel = null;

        canMove = true;

        _lastSentSteering = float.NaN;

        //cc.enabled = true;

        cameraPivot.localEulerAngles = Vector3.zero;

        pitch = 0f;

        BoatSteeringManager.Instance.SetSteering(0f);
    }

    public void StartCameraShake(float intensity, float duration)
    {
        StopCoroutine(nameof(CameraShakeRoutine));

        StartCoroutine(CameraShakeRoutine(intensity, duration));
    }

    private IEnumerator CameraShakeRoutine(float intensity, float duration)
    {
        Vector3 originalPos =
            cameraPivot.localPosition;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * intensity;

            cameraPivot.localPosition = originalPos + randomOffset;

            yield return null;
        }

        cameraPivot.localPosition = originalPos;
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"=== PLAYER DESPAWNED === OwnerClientId: {OwnerClientId}, IsOwner: {IsOwner}, IsSpawned: {IsSpawned}");
        Debug.Log($"Despawn called from:\n{System.Environment.StackTrace}");
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
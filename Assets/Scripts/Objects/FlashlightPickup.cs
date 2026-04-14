using UnityEngine;
using UnityEngine.Serialization;

public class FlashlightPickup : MonoBehaviour
{
    public enum DecayDirection
    {
        SlowToFast = 0,
        FastToSlow = 1
    }

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode toggleFlashlightKey = KeyCode.Tab;
    public float pickupDistance = 2.2f;
    public Transform player;
    public Camera playerCamera;

    [Header("Held View")]
    public Vector3 heldLocalPosition = new Vector3(0.284f, -0.211f, 0.385f);
    public Vector3 heldLocalEulerAngles = new Vector3(-5.694f, -3.655f, 4.079f);
    public Vector3 heldLocalScale = Vector3.one;
    public bool alignLightWithCameraForward = true;
    public bool alignLightOriginToCameraCenter = true;

    [Header("Light")]
    public Color flashlightColor = new Color(1f, 0.97f, 0.9f, 1f);
    [FormerlySerializedAs("flashlightIntensity")]
    public float flashlightEmission = 0.45f;
    public float flashlightRange = 18f;
    public float flashlightSpotAngle = 58f;
    public float flashlightInnerSpotAngle = 32f;
    public float lightForwardOffset = 0.08f;

    [Header("Drain & Recharge")]
    public float decayDurationSeconds = 5f;
    [Range(0f, 100f)] public float clickRechargePercent = 5f;
    [FormerlySerializedAs("slowToFastCurvePower")]
    public float decayCurvePower = 2f;
    public DecayDirection decayDirection = DecayDirection.SlowToFast;

    [Header("State")]
    [SerializeField] private bool pickedUp;
    [SerializeField] private bool isFlashlightEnabled = true;

    private Rigidbody rb;
    private Collider pickupCollider;
    private Light flashlightLight;
    private Transform worldModelAnchor;
    private Transform worldLightAnchor;
    private Renderer[] flashlightRenderers;
    private float currentPercent = 1f;
    private float decayElapsed;
    private const float FastToSlowTailRateFactor = 0.2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
        flashlightLight = GetComponentInChildren<Light>(true);
        flashlightRenderers = GetComponentsInChildren<Renderer>(true);
        EnsureFlashlightLightExists();
        ApplyFlashlightVisualState();
        ResolvePlayerAndCamera();
    }

    private void Update()
    {
        if (!pickedUp)
        {
            TryPickup();
            return;
        }

        HandleFlashlightToggle();

        if (isFlashlightEnabled)
        {
            UpdateDrainAndRecharge();
        }
    }

    private void TryPickup()
    {
        ResolvePlayerAndCamera();
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        if (distance > pickupDistance)
        {
            return;
        }

        if (!Input.GetKeyDown(pickupKey))
        {
            return;
        }

        PickUp();
    }

    private void LateUpdate()
    {
        if (!pickedUp)
        {
            return;
        }

        UpdateHeldTransformInWorldSpace();
    }

    private void PickUp()
    {
        if (pickedUp)
        {
            return;
        }

        ResolvePlayerAndCamera();
        if (playerCamera == null && player == null)
        {
            return;
        }

        EnsureFlashlightLightExists();
        pickedUp = true;
        isFlashlightEnabled = true;
        currentPercent = 1f;
        decayElapsed = 0f;

        DisablePhysicsAndCollisions();
        EnsureWorldAnchors();
        AttachToWorldAnchors();
        ApplyFlashlightVisualState();
        UpdateHeldTransformInWorldSpace();
    }

    private void OnDestroy()
    {
        if (worldModelAnchor != null)
        {
            Destroy(worldModelAnchor.gameObject);
        }

        if (worldLightAnchor != null)
        {
            Destroy(worldLightAnchor.gameObject);
        }
    }

    private void ResolvePlayerAndCamera()
    {
        if (player == null)
        {
            TryResolvePlayer();
        }

        if (playerCamera == null)
        {
            TryResolveCamera();
        }
    }

    private void TryResolvePlayer()
    {
#if UNITY_2023_1_OR_NEWER
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
#endif
        if (movement != null)
        {
            player = movement.transform;
            return;
        }

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            if (taggedPlayers[i] == null)
            {
                continue;
            }

            PlayerMovement movementOnTagged = taggedPlayers[i].GetComponent<PlayerMovement>();
            if (movementOnTagged == null)
            {
                movementOnTagged = taggedPlayers[i].GetComponentInParent<PlayerMovement>();
            }

            if (movementOnTagged == null)
            {
                movementOnTagged = taggedPlayers[i].GetComponentInChildren<PlayerMovement>();
            }

            if (movementOnTagged != null)
            {
                player = movementOnTagged.transform;
                return;
            }
        }

        if (taggedPlayers.Length > 0 && taggedPlayers[0] != null)
        {
            player = taggedPlayers[0].transform;
        }
    }

    private void TryResolveCamera()
    {
        if (player != null)
        {
            Transform cameraTransform = player.Find("CameraHolder/PlayerCam");
            if (cameraTransform != null)
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
            }

            if (playerCamera == null)
            {
                playerCamera = player.GetComponentInChildren<Camera>(true);
            }
        }

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerCamera = FindFirstObjectByType<Camera>();
#else
            playerCamera = FindObjectOfType<Camera>();
#endif
        }
    }

    private void EnsureWorldAnchors()
    {
        if (worldModelAnchor == null)
        {
            GameObject modelAnchorObject = new GameObject("FlashlightModelAnchor_World");
            worldModelAnchor = modelAnchorObject.transform;
        }

        if (worldLightAnchor == null && flashlightLight != null)
        {
            GameObject lightAnchorObject = new GameObject("FlashlightLightAnchor_World");
            worldLightAnchor = lightAnchorObject.transform;
        }

        if (worldModelAnchor != null)
        {
            worldModelAnchor.SetParent(null, false);
            worldModelAnchor.localScale = Vector3.one;
        }

        if (worldLightAnchor != null)
        {
            worldLightAnchor.SetParent(null, false);
            worldLightAnchor.localScale = Vector3.one;
        }
    }

    private void AttachToWorldAnchors()
    {
        if (worldModelAnchor != null && transform.parent != worldModelAnchor)
        {
            transform.SetParent(worldModelAnchor, false);
        }

        if (flashlightLight != null && worldLightAnchor != null && flashlightLight.transform.parent != worldLightAnchor)
        {
            flashlightLight.transform.SetParent(worldLightAnchor, false);
        }
    }

    private void UpdateHeldTransformInWorldSpace()
    {
        ResolvePlayerAndCamera();
        if (playerCamera == null)
        {
            return;
        }

        EnsureFlashlightLightExists();
        EnsureWorldAnchors();
        AttachToWorldAnchors();

        Vector3 cameraPosition = playerCamera.transform.position;
        Quaternion cameraWorldRotation = playerCamera.transform.rotation;

        if (worldModelAnchor != null)
        {
            worldModelAnchor.position = cameraPosition;
            worldModelAnchor.rotation = cameraWorldRotation;
            worldModelAnchor.localScale = Vector3.one;
        }

        transform.localPosition = heldLocalPosition;
        transform.localRotation = Quaternion.Euler(heldLocalEulerAngles);
        transform.localScale = heldLocalScale;
        ApplyFlashlightVisualState();

        if (!isFlashlightEnabled)
        {
            return;
        }

        ApplyCurrentLightIntensity();

        if (flashlightLight == null || worldLightAnchor == null)
        {
            return;
        }

        if (alignLightWithCameraForward)
        {
            Vector3 lightOrigin = alignLightOriginToCameraCenter
                ? cameraPosition
                : (cameraPosition + (playerCamera.transform.right * heldLocalPosition.x) + (playerCamera.transform.up * heldLocalPosition.y) + (playerCamera.transform.forward * heldLocalPosition.z));
            worldLightAnchor.position = lightOrigin + (playerCamera.transform.forward * lightForwardOffset);
            worldLightAnchor.rotation = cameraWorldRotation;
        }
        else
        {
            worldLightAnchor.position = transform.position + (playerCamera.transform.forward * lightForwardOffset);
            worldLightAnchor.rotation = transform.rotation;
        }

        worldLightAnchor.localScale = Vector3.one;

        flashlightLight.transform.localPosition = Vector3.zero;
        flashlightLight.transform.localRotation = Quaternion.identity;
        flashlightLight.transform.localScale = Vector3.one;
        flashlightLight.gameObject.SetActive(true);
        flashlightLight.enabled = true;
    }

    private void EnsureFlashlightLightExists()
    {
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
        }

        if (flashlightLight == null)
        {
            GameObject lightObject = new GameObject("FlashlightLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            lightObject.transform.localRotation = Quaternion.identity;
            flashlightLight = lightObject.AddComponent<Light>();
        }

        ConfigureFlashlightLight(flashlightLight);
        ApplyFlashlightVisualState();
    }

    private void ConfigureFlashlightLight(Light targetLight)
    {
        if (targetLight == null)
        {
            return;
        }

        targetLight.type = LightType.Spot;
        targetLight.color = flashlightColor;
        targetLight.range = Mathf.Max(0.1f, flashlightRange);
        targetLight.spotAngle = Mathf.Clamp(flashlightSpotAngle, 1f, 179f);
        targetLight.innerSpotAngle = Mathf.Clamp(flashlightInnerSpotAngle, 0f, targetLight.spotAngle);
        targetLight.cullingMask = ~0;
        targetLight.renderMode = LightRenderMode.Auto;
        targetLight.enabled = true;

        ApplyCurrentLightIntensity();
    }

    private void ApplyCurrentLightIntensity()
    {
        if (flashlightLight == null)
        {
            return;
        }

        float maxEmission = Mathf.Max(0.01f, flashlightEmission);
        currentPercent = Mathf.Clamp01(currentPercent);
        flashlightLight.intensity = maxEmission * currentPercent;
    }

    private void HandleFlashlightToggle()
    {
        if (!Input.GetKeyDown(toggleFlashlightKey))
        {
            return;
        }

        isFlashlightEnabled = !isFlashlightEnabled;
        ApplyFlashlightVisualState();
    }

    private void ApplyFlashlightVisualState()
    {
        if (flashlightRenderers == null || flashlightRenderers.Length == 0)
        {
            flashlightRenderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < flashlightRenderers.Length; i++)
        {
            Renderer renderer = flashlightRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = isFlashlightEnabled;
        }

        if (flashlightLight != null)
        {
            flashlightLight.gameObject.SetActive(isFlashlightEnabled);
            flashlightLight.enabled = isFlashlightEnabled;
        }
    }

    private void UpdateDrainAndRecharge()
    {
        if (Input.GetMouseButtonDown(0))
        {
            float recharge = Mathf.Max(0f, clickRechargePercent) / 100f;
            currentPercent = Mathf.Clamp01(currentPercent + recharge);
        }

        float duration = Mathf.Max(0.01f, decayDurationSeconds);
        float curvePower = Mathf.Max(0.01f, decayCurvePower);
        float normalizedTime = Mathf.Clamp01(decayElapsed / duration);
        float fastestDrainRate = (curvePower + 1f) / duration;
        float curveInput = decayDirection == DecayDirection.SlowToFast
            ? normalizedTime
            : (1f - normalizedTime);
        float drainRate = fastestDrainRate * Mathf.Pow(Mathf.Clamp01(curveInput), curvePower);

        if (decayElapsed >= duration)
        {
            drainRate = decayDirection == DecayDirection.SlowToFast
                ? fastestDrainRate
                : (fastestDrainRate * FastToSlowTailRateFactor);
        }

        currentPercent = Mathf.Clamp01(currentPercent - (drainRate * Time.deltaTime));
        decayElapsed += Time.deltaTime;
        ApplyCurrentLightIntensity();
    }

    private void OnValidate()
    {
        heldLocalScale = new Vector3(
            Mathf.Max(0.001f, heldLocalScale.x),
            Mathf.Max(0.001f, heldLocalScale.y),
            Mathf.Max(0.001f, heldLocalScale.z));
        flashlightEmission = Mathf.Max(0.01f, flashlightEmission);
        flashlightRange = Mathf.Max(0.1f, flashlightRange);
        flashlightSpotAngle = Mathf.Clamp(flashlightSpotAngle, 1f, 179f);
        flashlightInnerSpotAngle = Mathf.Clamp(flashlightInnerSpotAngle, 0f, flashlightSpotAngle);
        lightForwardOffset = Mathf.Max(0f, lightForwardOffset);
        decayDurationSeconds = Mathf.Max(0.01f, decayDurationSeconds);
        clickRechargePercent = Mathf.Clamp(clickRechargePercent, 0f, 100f);
        decayCurvePower = Mathf.Max(0.01f, decayCurvePower);

        if (!Application.isPlaying)
        {
            currentPercent = 1f;
            decayElapsed = 0f;
        }

        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>(true);
        }

        if (flashlightLight != null)
        {
            ConfigureFlashlightLight(flashlightLight);
        }

        ApplyFlashlightVisualState();

        if (Application.isPlaying && pickedUp)
        {
            UpdateHeldTransformInWorldSpace();
        }
    }

    private void DisablePhysicsAndCollisions()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Destroy(rb);
            rb = null;
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        RemoveAllColliders();
    }

    private void RemoveAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            colliders[i].enabled = false;
            Destroy(colliders[i]);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    private const string SonarCanvasName = "SonarUICanvas";
    private const string SonarRootName = "SonarCooldownUI";

    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float groundDrag;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Sonar")]
    public GameObject vfxPrefab;
    public Vector3 vfxOffset = new Vector3(0, 1, 0);
    public float sonarIntervals;
    public float sonarCooldown = 1.5f;
    public AudioClip sonarSound;  // Sound for sonar scanning

    [Header("Sonar UI")]
    public Image sonarCooldownFillImage;
    public Image sonarCooldownBackgroundImage;
    public Vector2 sonarIconSize = new Vector2(72f, 72f);
    public Vector2 sonarIconOffset = new Vector2(-32f, 32f);
    public Color sonarReadyColor = new Color(0.25f, 1f, 0.95f, 0.95f);
    public Color sonarCooldownColor = new Color(0.25f, 0.75f, 1f, 0.45f);
    public Color sonarBackgroundColor = new Color(0f, 0f, 0f, 0.4f);

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode sonarKey = KeyCode.E;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    public float horizontalInput;
    public float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
    }

    [Header("Audio")]
    public AudioSource audioSource;  // Main AudioSource to play sounds
    public AudioClip walkSound;  // Sound for walking
    public AudioClip sprintSound;  // Sound for sprinting
    public float walkFootstepInterval = 0.5f;  // Time between footsteps when walking
    public float sprintFootstepInterval = 0.3f;  // Time between footsteps when sprinting
    private float footstepTimer = 0f;  // Timer for footstep sounds
    private float sonarCooldownTimer = 0f;
    private RectTransform sonarCooldownRoot;

    private static Sprite generatedSonarCircleSprite;
    private static Texture2D generatedSonarCircleTexture;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;
        sonarCooldown = Mathf.Max(0f, sonarCooldown);
        EnsureSonarCooldownUI();
        UpdateSonarCooldownUI();
    }

    private void Update()
    {
        // Ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();
        HandleSonarCooldown();

        // Handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        // Play footstep sounds based on movement state
        PlayFootstepSounds();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        // Mode - Crouching
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Mode - Sprinting
        else if (Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Mode - Walking
        else
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void HandleSonarCooldown()
    {
        if (sonarCooldownTimer > 0f)
        {
            sonarCooldownTimer = Mathf.Max(0f, sonarCooldownTimer - Time.deltaTime);
        }

        if (Input.GetKeyDown(sonarKey) && sonarCooldownTimer <= 0f)
        {
            SonarScan();
            sonarCooldownTimer = sonarCooldown;
        }

        UpdateSonarCooldownUI();
    }

    private void UpdateSonarCooldownUI()
    {
        EnsureSonarCooldownUI();

        if (sonarCooldownFillImage == null)
        {
            return;
        }

        float normalizedCooldown = sonarCooldown <= 0f ? 0f : sonarCooldownTimer / sonarCooldown;
        bool onCooldown = sonarCooldown > 0f && sonarCooldownTimer > 0f;

        sonarCooldownFillImage.fillAmount = onCooldown ? normalizedCooldown : 1f;
        sonarCooldownFillImage.color = onCooldown ? sonarCooldownColor : sonarReadyColor;

        if (sonarCooldownBackgroundImage != null)
        {
            sonarCooldownBackgroundImage.color = sonarBackgroundColor;
        }
    }

    private void SonarScan()
    {
        // Play sonar sound
        if (sonarSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonarSound);
        }

        // Visual effect for sonar scan
        Vector3 spawnPosition = transform.position + vfxOffset;
        Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);

        StartCoroutine(SpawnAdditionalVFX(spawnPosition));
    }

    private IEnumerator SpawnAdditionalVFX(Vector3 initialPosition)
    {
        yield return new WaitForSeconds(sonarIntervals);

        Vector3 secondPosition = initialPosition;
        Instantiate(vfxPrefab, secondPosition, Quaternion.identity);

        yield return new WaitForSeconds(sonarIntervals);

        Vector3 thirdPosition = initialPosition;
        Instantiate(vfxPrefab, thirdPosition, Quaternion.identity);
    }

    private void PlayFootstepSounds()
    {
        // If player is grounded, moving, and not crouching
        if (grounded && (horizontalInput != 0 || verticalInput != 0) && state != MovementState.crouching)
        {
            footstepTimer -= Time.deltaTime;

            // If the footstep timer has expired, play the appropriate sound
            if (footstepTimer <= 0)
            {
                if (state == MovementState.sprinting && sprintSound != null)
                {
                    audioSource.PlayOneShot(sprintSound);
                    footstepTimer = sprintFootstepInterval;  // Sprinting footstep interval
                }
                else if (state == MovementState.walking && walkSound != null)
                {
                    audioSource.PlayOneShot(walkSound);
                    footstepTimer = walkFootstepInterval;  // Walking footstep interval
                }
            }
        }
        else
        {
            // Reset the timer when the player is not moving or grounded
            footstepTimer = 0f;
        }
    }

    private void EnsureSonarCooldownUI()
    {
        if (IsExistingSonarUIValid())
        {
            if (sonarCooldownRoot == null)
            {
                sonarCooldownRoot = sonarCooldownFillImage.transform.parent as RectTransform;
            }

            ApplyRootLayout(sonarCooldownRoot);
            return;
        }

        sonarCooldownFillImage = null;
        sonarCooldownBackgroundImage = null;
        sonarCooldownRoot = null;

        RectTransform canvasRect = ResolveUICanvasRect();
        if (canvasRect == null)
        {
            return;
        }

        Transform existingRoot = canvasRect.Find(SonarRootName);
        if (existingRoot != null)
        {
            sonarCooldownRoot = existingRoot as RectTransform;
        }
        else
        {
            GameObject rootObject = new GameObject(SonarRootName, typeof(RectTransform));
            sonarCooldownRoot = rootObject.GetComponent<RectTransform>();
            sonarCooldownRoot.SetParent(canvasRect, false);
        }

        ApplyRootLayout(sonarCooldownRoot);

        Transform bgTransform = sonarCooldownRoot.Find("Background");
        if (bgTransform == null)
        {
            GameObject bgObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgObject.transform.SetParent(sonarCooldownRoot, false);
            bgTransform = bgObject.transform;
        }

        Transform fillTransform = sonarCooldownRoot.Find("Fill");
        if (fillTransform == null)
        {
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(sonarCooldownRoot, false);
            fillTransform = fillObject.transform;
        }

        RectTransform bgRect = bgTransform as RectTransform;
        RectTransform fillRect = fillTransform as RectTransform;
        StretchToRoot(bgRect);
        StretchToRoot(fillRect);

        Sprite circleSprite = GetOrCreateCircleSprite();

        sonarCooldownBackgroundImage = bgTransform.GetComponent<Image>();
        sonarCooldownBackgroundImage.sprite = circleSprite;
        sonarCooldownBackgroundImage.type = Image.Type.Simple;
        sonarCooldownBackgroundImage.color = sonarBackgroundColor;
        sonarCooldownBackgroundImage.raycastTarget = false;

        sonarCooldownFillImage = fillTransform.GetComponent<Image>();
        sonarCooldownFillImage.sprite = circleSprite;
        sonarCooldownFillImage.type = Image.Type.Filled;
        sonarCooldownFillImage.fillMethod = Image.FillMethod.Radial360;
        sonarCooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
        sonarCooldownFillImage.fillClockwise = false;
        sonarCooldownFillImage.raycastTarget = false;
    }

    private RectTransform ResolveUICanvasRect()
    {
        Canvas targetCanvas = null;

        GameObject existingCanvasObject = GameObject.Find(SonarCanvasName);
        if (existingCanvasObject != null)
        {
            targetCanvas = existingCanvasObject.GetComponent<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject(SonarCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasObject.GetComponent<Canvas>();
        }

        ConfigureSonarCanvas(targetCanvas);
        return targetCanvas.GetComponent<RectTransform>();
    }

    private bool IsExistingSonarUIValid()
    {
        if (sonarCooldownFillImage == null || sonarCooldownBackgroundImage == null)
        {
            return false;
        }

        if (!sonarCooldownFillImage.gameObject.activeInHierarchy || !sonarCooldownBackgroundImage.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas parentCanvas = sonarCooldownFillImage.GetComponentInParent<Canvas>();
        if (parentCanvas == null || !parentCanvas.isActiveAndEnabled || parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            return false;
        }

        if (HasInvisibleCanvasGroupInHierarchy(sonarCooldownFillImage.transform))
        {
            return false;
        }

        return true;
    }

    private void ConfigureSonarCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        GameObject canvasObject = canvas.gameObject;
        if (!canvasObject.activeSelf)
        {
            canvasObject.SetActive(true);
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private bool HasInvisibleCanvasGroupInHierarchy(Transform root)
    {
        Transform current = root;
        while (current != null)
        {
            CanvasGroup canvasGroup = current.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0.01f)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ApplyRootLayout(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = sonarIconOffset;
        root.sizeDelta = sonarIconSize;
        root.localScale = Vector3.one;
        root.SetAsLastSibling();
    }

    private void StretchToRoot(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private Sprite GetOrCreateCircleSprite()
    {
        if (generatedSonarCircleSprite != null)
        {
            return generatedSonarCircleSprite;
        }

        const int textureSize = 128;
        generatedSonarCircleTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        generatedSonarCircleTexture.name = "SonarCooldownCircleTexture";

        float outerRadius = (textureSize * 0.5f) - 1f;
        float innerRadius = outerRadius * 0.58f;
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);

        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                float outerEdgeAlpha = Mathf.Clamp01(outerRadius - distance);
                float innerEdgeAlpha = Mathf.Clamp01(distance - innerRadius);
                float alpha = Mathf.Min(outerEdgeAlpha, innerEdgeAlpha);

                pixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        generatedSonarCircleTexture.SetPixels(pixels);
        generatedSonarCircleTexture.Apply();

        generatedSonarCircleSprite = Sprite.Create(
            generatedSonarCircleTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize
        );

        return generatedSonarCircleSprite;
    }
}

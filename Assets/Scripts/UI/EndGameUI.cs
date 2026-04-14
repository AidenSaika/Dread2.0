using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EndSceneTrigger : MonoBehaviour
{
    public GameObject player;
    public CanvasGroup whiteScreenOverlay;  // UI Canvas with an Image that will turn white
    public float fadeDuration = 2f;         // Duration for the screen to fade to white
    public float soundFadeDuration = 2f;    // Duration for the sound to fade out
    public string endingSceneName = "Ending";
    public string levelSceneName = "LevelOne";
    public Transform fixedEndPoint;
    public float fixedEndPointRadius = 1f;
    public int fadeOverlaySortingOrder = 2000;
    public float endZoneDistanceFallback = 3f;
    public bool startOnTriggerEnter = true;
    public bool requireReachFixedEndPoint = false;

    private bool playerInEndZone = false;
    private bool endSequenceStarted = false;
    private Transform playerRoot;
    private CanvasGroup runtimeWhiteOverlay;
    private Collider endZoneCollider;

    private void Awake()
    {
        endZoneCollider = GetComponent<Collider>();
        ResolvePlayerReference();
        ResolveOverlayReference();
    }

    private void Start()
    {
        ResolvePlayerReference();
        ResolveOverlayReference();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (endSequenceStarted)
        {
            return;
        }

        if (!IsLevelOnePlayerCollider(other))
        {
            return;
        }

        playerInEndZone = true;

        if (startOnTriggerEnter)
        {
            TryStartEndSequence();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (endSequenceStarted)
        {
            return;
        }

        if (!IsLevelOnePlayerCollider(other))
        {
            return;
        }

        playerInEndZone = false;
    }

    private void Update()
    {
        if (endSequenceStarted)
        {
            return;
        }

        if (!playerInEndZone)
        {
            if (IsAllowedScene() && IsPlayerInsideEndZone())
            {
                playerInEndZone = true;
            }
            else
            {
                return;
            }
        }

        if (startOnTriggerEnter)
        {
            TryStartEndSequence();
            return;
        }

        if (requireReachFixedEndPoint && !HasReachedFixedEndPoint())
        {
            return;
        }

        TryStartEndSequence();
    }

    IEnumerator EndGameSequence()
    {
        // Start fading out sound in parallel.
        StartCoroutine(FadeOutSound());

        // Fade to full white first, then jump scene.
        yield return StartCoroutine(FadeToWhite());

        SceneManager.LoadScene(endingSceneName);
    }

    IEnumerator FadeToWhite()
    {
        CanvasGroup overlay = ResolveFadeOverlay();
        if (overlay == null)
        {
            yield break;
        }

        overlay.gameObject.SetActive(true);
        overlay.alpha = 0f;

        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            // Gradually increase the alpha value of the white overlay
            overlay.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }
        overlay.alpha = 1f; // Ensure fully white at the end
    }

    IEnumerator FadeOutSound()
    {
        float startVolume = AudioListener.volume;
        float time = 0f;
        while (time < soundFadeDuration)
        {
            time += Time.unscaledDeltaTime;
            // Gradually reduce the global audio volume
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, time / soundFadeDuration);
            yield return null;
        }
        AudioListener.volume = 0f; // Ensure sound is fully muted at the end
    }

    private bool IsLevelOnePlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (!IsAllowedScene())
        {
            return false;
        }

        if (playerRoot == null)
        {
            ResolvePlayerReference();
        }

        Transform otherTransform = other.transform;
        if (playerRoot != null)
        {
            return otherTransform == playerRoot || otherTransform.IsChildOf(playerRoot);
        }

        if (!otherTransform.CompareTag("Player"))
        {
            return false;
        }

#if UNITY_2023_1_OR_NEWER
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
#endif
        if (movement == null)
        {
            return true;
        }

        return otherTransform == movement.transform || otherTransform.IsChildOf(movement.transform);
    }

    private bool IsAllowedScene()
    {
        if (string.IsNullOrEmpty(levelSceneName))
        {
            return true;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string normalizedScene = NormalizeSceneName(sceneName);
        string normalizedTarget = NormalizeSceneName(levelSceneName);

        if (string.IsNullOrEmpty(normalizedTarget))
        {
            return true;
        }

        return normalizedScene.StartsWith(normalizedTarget, System.StringComparison.Ordinal);
    }

    private bool HasReachedFixedEndPoint()
    {
        if (playerRoot == null)
        {
            ResolvePlayerReference();
            if (playerRoot == null)
            {
                return false;
            }
        }

        Transform target = fixedEndPoint != null ? fixedEndPoint : transform;
        Vector3 playerPosition = playerRoot.position;
        Vector3 endPointPosition = target.position;

        Vector2 playerXZ = new Vector2(playerPosition.x, playerPosition.z);
        Vector2 endXZ = new Vector2(endPointPosition.x, endPointPosition.z);
        return Vector2.Distance(playerXZ, endXZ) <= Mathf.Max(0.01f, fixedEndPointRadius);
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
        {
            playerRoot = player.transform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
#endif
        if (movement != null)
        {
            player = movement.gameObject;
            playerRoot = movement.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer;
            playerRoot = taggedPlayer.transform;
        }
    }

    private void ResolveOverlayReference()
    {
        // Kept for backward compatibility in inspector; runtime fade uses a dedicated overlay.
    }

    private CanvasGroup ResolveFadeOverlay()
    {
        if (runtimeWhiteOverlay == null)
        {
            runtimeWhiteOverlay = CreateRuntimeWhiteOverlay();
        }

        return runtimeWhiteOverlay;
    }

    private CanvasGroup CreateRuntimeWhiteOverlay()
    {
        GameObject overlayCanvasObject = new GameObject(
            "EndFadeOverlayCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup)
        );

        Canvas canvas = overlayCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = fadeOverlaySortingOrder;

        CanvasScaler scaler = overlayCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = overlayCanvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        RectTransform canvasRect = overlayCanvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("WhiteImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(overlayCanvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        CanvasGroup overlay = overlayCanvasObject.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.interactable = false;
        overlay.blocksRaycasts = false;
        return overlay;
    }

    private bool IsPlayerInsideEndZone()
    {
        if (endSequenceStarted || !IsAllowedScene())
        {
            return false;
        }

        if (playerRoot == null)
        {
            ResolvePlayerReference();
            if (playerRoot == null)
            {
                return false;
            }
        }

        Vector3 playerPosition = playerRoot.position;

        if (endZoneCollider != null)
        {
            Vector3 closestPoint = endZoneCollider.ClosestPoint(playerPosition);
            if ((closestPoint - playerPosition).sqrMagnitude <= 0.0001f)
            {
                return true;
            }
        }

        Vector2 playerXZ = new Vector2(playerPosition.x, playerPosition.z);
        Vector2 zoneXZ = new Vector2(transform.position.x, transform.position.z);
        return Vector2.Distance(playerXZ, zoneXZ) <= Mathf.Max(0.2f, endZoneDistanceFallback);
    }

    private void TryStartEndSequence()
    {
        if (endSequenceStarted)
        {
            return;
        }

        endSequenceStarted = true;
        StartCoroutine(EndGameSequence());
    }

    private string NormalizeSceneName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(rawName.Length);
        for (int i = 0; i < rawName.Length; i++)
        {
            char c = rawName[i];
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}

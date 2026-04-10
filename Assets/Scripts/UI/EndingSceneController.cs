using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSceneController : MonoBehaviour
{
    [Header("Scene")]
    public string restartSceneName = "LevelOne";

    [Header("UI References (Optional)")]
    public Canvas endingCanvas;
    public Image backgroundImage;
    public Button restartButton;
    public Button quitButton;

    [Header("Button Labels")]
    public string restartButtonText = "Restart";
    public string quitButtonText = "Quit";

    [Header("Style")]
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
    public Color buttonColor = new Color(0.2f, 0.25f, 0.35f, 1f);
    public Color buttonTextColor = new Color(1f, 1f, 1f, 1f);
    public Vector2 buttonSize = new Vector2(320f, 80f);
    public float buttonVerticalOffset = 52f;

    private const string EndingSceneName = "Ending";

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != EndingSceneName)
        {
            Destroy(gameObject);
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        EnsureSceneUI();
        BindButtonEvents();
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(null);
            return;
        }

        BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
        if (modules == null || modules.Length == 0)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void EnsureSceneUI()
    {
        if (endingCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("EndingCanvas");
            if (canvasObject != null)
            {
                endingCanvas = canvasObject.GetComponent<Canvas>();
            }
        }

        if (endingCanvas == null)
        {
            endingCanvas = CreateCanvas();
        }

        RectTransform canvasRect = endingCanvas.GetComponent<RectTransform>();
        EnsureBackground(canvasRect);

        restartButton = EnsureButton(
            restartButton,
            "RestartButton",
            new Vector2(0f, buttonVerticalOffset),
            restartButtonText,
            canvasRect
        );

        quitButton = EnsureButton(
            quitButton,
            "QuitButton",
            new Vector2(0f, -buttonVerticalOffset),
            quitButtonText,
            canvasRect
        );
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        return canvas;
    }

    private void EnsureBackground(RectTransform canvasRect)
    {
        if (backgroundImage == null)
        {
            Transform existingBackground = canvasRect.Find("Background");
            if (existingBackground != null)
            {
                backgroundImage = existingBackground.GetComponent<Image>();
            }
        }

        if (backgroundImage == null)
        {
            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(canvasRect, false);
            backgroundImage = backgroundObject.GetComponent<Image>();
        }

        RectTransform backgroundRect = backgroundImage.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = Vector2.zero;

        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;
    }

    private Button EnsureButton(Button button, string objectName, Vector2 anchoredPosition, string labelText, RectTransform canvasRect)
    {
        if (button == null)
        {
            Transform existing = canvasRect.Find(objectName);
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
        }

        if (button == null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasRect, false);
            button = buttonObject.GetComponent<Button>();
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = buttonSize;

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }
        image.color = buttonColor;

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = new Color(buttonColor.r + 0.08f, buttonColor.g + 0.08f, buttonColor.b + 0.08f, 1f);
        colors.pressedColor = new Color(buttonColor.r - 0.05f, buttonColor.g - 0.05f, buttonColor.b - 0.05f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        button.colors = colors;

        button.targetGraphic = image;

        EnsureButtonLabel(button.transform, labelText);
        return button;
    }

    private void EnsureButtonLabel(Transform buttonTransform, string labelText)
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Transform labelTransform = buttonTransform.Find("Label");
        Text label = null;
        if (labelTransform != null)
        {
            label = labelTransform.GetComponent<Text>();
        }

        if (label == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonTransform, false);
            label = labelObject.GetComponent<Text>();
            labelTransform = labelObject.transform;
        }

        RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.font = uiFont;
        label.text = labelText;
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 34;
        label.fontStyle = FontStyle.Bold;
        label.color = buttonTextColor;
    }

    private void BindButtonEvents()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void RestartGame()
    {
        AudioListener.volume = 1f;
        SceneManager.LoadScene(restartSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

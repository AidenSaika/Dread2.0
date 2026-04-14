#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

public static class LevelOnePlayerPrefabBuilder
{
    private const string PrefabDirectory = "Assets/Player";
    private const string PrefabPath = PrefabDirectory + "/LevelOnePlayer.prefab";

    private const string SonarVfxGuid = "4e06cb372b880aa42b454d6ada20d1e2";
    private const string SonarSoundGuid = "b22ca754a5dfe45448fc23b5f51f4d07";
    private const string WalkSoundGuid = "33aa518bf06031e46aae11b530d061f3";
    private const string SprintSoundGuid = "d20106412c421ea44b32c6d3cd5363b1";
    private const string PlayerPhysicsMaterialGuid = "f6ba91a9fb0428b4b81ceb91d84b5273";

    [MenuItem("Tools/Player/Create LevelOne Player Prefab")]
    public static void CreateOrUpdatePlayerPrefabFromMenu()
    {
        CreateOrUpdatePlayerPrefab(logResult: true);
    }

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExistsAfterCompile()
    {
        EditorApplication.delayCall += TryAutoCreatePrefab;
    }

    private static void TryAutoCreatePrefab()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existingPrefab == null)
        {
            CreateOrUpdatePlayerPrefab(logResult: false);
            return;
        }

        if (HasRequiredPlayerSystems(existingPrefab))
        {
            return;
        }

        CreateOrUpdatePlayerPrefab(logResult: false);
    }

    private static void CreateOrUpdatePlayerPrefab(bool logResult)
    {
        EnsureFolderExists(PrefabDirectory);

        GameObject playerRoot = new GameObject("Player");
        try
        {
            BuildPlayerHierarchy(playerRoot);

            PrefabUtility.SaveAsPrefabAsset(playerRoot, PrefabPath, out bool success);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logResult)
            {
                if (success)
                {
                    Debug.Log($"[PLAYER PREFAB] Created/Updated: {PrefabPath}");
                }
                else
                {
                    Debug.LogError($"[PLAYER PREFAB] Failed to create/update: {PrefabPath}");
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(playerRoot);
        }
    }

    private static void BuildPlayerHierarchy(GameObject playerRoot)
    {
        int playerLayer = GetLayerOrFallback("Player", 6);
        int uiLayer = GetLayerOrFallback("UI", 5);

        playerRoot.layer = playerLayer;
        if (TagExists("Player"))
        {
            playerRoot.tag = "Player";
        }

        Transform playerTransform = playerRoot.transform;
        playerTransform.localPosition = Vector3.zero;
        playerTransform.localRotation = Quaternion.identity;
        playerTransform.localScale = Vector3.one;

        Rigidbody rb = playerRoot.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        CapsuleCollider playerCollider = playerRoot.AddComponent<CapsuleCollider>();
        playerCollider.radius = 0.5f;
        playerCollider.height = 2.4f;
        playerCollider.center = Vector3.zero;
        playerCollider.direction = 1;
        playerCollider.sharedMaterial = LoadAssetByGuid<PhysicMaterial>(PlayerPhysicsMaterialGuid);

        AudioSource movementAudio = playerRoot.AddComponent<AudioSource>();
        ConfigureAudioSource(movementAudio, volume: 0.5f);

        AudioSource extraAudioA = playerRoot.AddComponent<AudioSource>();
        ConfigureAudioSource(extraAudioA, volume: 1f);

        AudioSource extraAudioB = playerRoot.AddComponent<AudioSource>();
        ConfigureAudioSource(extraAudioB, volume: 1f);

        Transform playerObj = CreateChildTransform("PlayerObj", playerTransform, new Vector3(0f, -0.464f, 0f), playerLayer);
        if (TagExists("Player"))
        {
            playerObj.gameObject.tag = "Player";
        }

        CapsuleCollider playerObjCollider = playerObj.gameObject.AddComponent<CapsuleCollider>();
        playerObjCollider.radius = 0.5f;
        playerObjCollider.height = 2.4f;
        playerObjCollider.center = Vector3.zero;
        playerObjCollider.direction = 1;
        playerObjCollider.enabled = false;
        playerObjCollider.sharedMaterial = LoadAssetByGuid<PhysicMaterial>(PlayerPhysicsMaterialGuid);

        Transform orientation = CreateChildTransform("Orientation", playerTransform, new Vector3(0f, 0.115f, 0f), playerLayer);
        Transform cameraPos = CreateChildTransform("CameraPos", playerTransform, new Vector3(0f, 0.311f, 0f), playerLayer);
        Transform cameraHolder = CreateChildTransform("CameraHolder", playerTransform, Vector3.zero, playerLayer);

        MoveCam moveCam = cameraHolder.gameObject.AddComponent<MoveCam>();
        moveCam.cameraPosition = cameraPos;

        GameObject playerCamGo = new GameObject("PlayerCam");
        playerCamGo.layer = playerLayer;
        if (TagExists("MainCamera"))
        {
            playerCamGo.tag = "MainCamera";
        }
        playerCamGo.transform.SetParent(cameraHolder, false);

        Camera playerCamera = playerCamGo.AddComponent<Camera>();
        playerCamera.nearClipPlane = 0.01f;
        playerCamera.farClipPlane = 1000f;
        playerCamera.fieldOfView = 60f;
        playerCamera.clearFlags = CameraClearFlags.Skybox;
        playerCamera.allowHDR = true;
        playerCamera.allowMSAA = true;
        playerCamera.depth = -1f;

        playerCamGo.AddComponent<AudioListener>();
        AddUrpCameraDataIfAvailable(playerCamGo);

        PlayerCam playerCam = playerCamGo.AddComponent<PlayerCam>();
        playerCam.sensX = 400f;
        playerCam.sensY = 400f;
        playerCam.orientation = orientation;

        HeadbobController headbob = playerCamGo.AddComponent<HeadbobController>();
        headbob.walkBobSpeed = 8f;
        headbob.walkBobAmount = 0.06f;
        headbob.runBobSpeed = 14f;
        headbob.runBobAmount = 0.15f;
        headbob.crouchBobSpeed = 0f;
        headbob.crouchBobAmount = 0f;

        Canvas sonarCanvas;
        Image sonarBg;
        Image sonarFill;
        Text sonarKeyLabel;
        BuildSonarCanvas(playerTransform, uiLayer, out sonarCanvas, out sonarBg, out sonarFill, out sonarKeyLabel);

        PlayerMovement movement = playerRoot.AddComponent<PlayerMovement>();
        movement.walkSpeed = 3.5f;
        movement.sprintSpeed = 8f;
        movement.groundDrag = 11f;
        movement.crouchSpeed = 1.5f;
        movement.crouchYScale = 0.1f;
        movement.vfxPrefab = LoadAssetByGuid<GameObject>(SonarVfxGuid);
        movement.vfxOffset = new Vector3(0f, 1f, 0f);
        movement.sonarIntervals = 0.3f;
        movement.sonarCooldown = 2f;
        movement.sonarSound = LoadAssetByGuid<AudioClip>(SonarSoundGuid);
        movement.sonarCooldownFillImage = sonarFill;
        movement.sonarCooldownBackgroundImage = sonarBg;
        movement.sonarKeyLabel = sonarKeyLabel;
        movement.sonarIconSize = new Vector2(72f, 72f);
        movement.sonarIconOffset = new Vector2(-32f, 32f);
        movement.sonarReadyColor = new Color(0.25f, 1f, 0.95f, 0.95f);
        movement.sonarCooldownColor = new Color(0.25f, 0.75f, 1f, 0.45f);
        movement.sonarBackgroundColor = new Color(0f, 0f, 0f, 0.4f);
        movement.sonarKeyLabelFontSize = 42;
        movement.sonarKeyLabelColor = Color.white;
        movement.sprintKey = KeyCode.LeftShift;
        movement.crouchKey = KeyCode.LeftControl;
        movement.sonarKey = KeyCode.E;
        movement.playerHeight = 2.4f;
        movement.whatIsGround = 1 << 3;
        movement.orientation = orientation;
        movement.audioSource = movementAudio;
        movement.walkSound = LoadAssetByGuid<AudioClip>(WalkSoundGuid);
        movement.sprintSound = LoadAssetByGuid<AudioClip>(SprintSoundGuid);
        movement.walkFootstepInterval = 0.8f;
        movement.sprintFootstepInterval = 0.4f;

        if (sonarCanvas != null)
        {
            sonarCanvas.gameObject.SetActive(true);
        }
    }

    private static void BuildSonarCanvas(
        Transform playerRoot,
        int uiLayer,
        out Canvas canvas,
        out Image backgroundImage,
        out Image fillImage,
        out Text keyLabel)
    {
        GameObject canvasGo = new GameObject("SonarUICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(playerRoot, false);
        SetLayerRecursively(canvasGo, uiLayer);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject rootGo = new GameObject("SonarCooldownUI", typeof(RectTransform));
        rootGo.transform.SetParent(canvasRect, false);
        RectTransform rootRect = rootGo.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.sizeDelta = new Vector2(72f, 72f);
        rootRect.anchoredPosition = new Vector2(-32f, 32f);

        Sprite uiCircle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(rootRect, false);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        StretchRect(bgRect);
        backgroundImage = bgGo.GetComponent<Image>();
        backgroundImage.sprite = uiCircle;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = new Color(0f, 0f, 0f, 0.4f);
        backgroundImage.raycastTarget = false;

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(rootRect, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        StretchRect(fillRect);
        fillImage = fillGo.GetComponent<Image>();
        fillImage.sprite = uiCircle;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = false;
        fillImage.fillAmount = 1f;
        fillImage.color = new Color(0.25f, 1f, 0.95f, 0.95f);
        fillImage.raycastTarget = false;

        GameObject keyLabelGo = new GameObject("KeyLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        keyLabelGo.transform.SetParent(rootRect, false);
        RectTransform keyLabelRect = keyLabelGo.GetComponent<RectTransform>();
        StretchRect(keyLabelRect);
        keyLabel = keyLabelGo.GetComponent<Text>();
        keyLabel.text = "E";
        keyLabel.alignment = TextAnchor.MiddleCenter;
        keyLabel.fontStyle = FontStyle.Bold;
        keyLabel.fontSize = 42;
        keyLabel.color = Color.white;
        keyLabel.raycastTarget = false;
        keyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyLabel.verticalOverflow = VerticalWrapMode.Overflow;
        keyLabel.resizeTextForBestFit = false;
        keyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureAudioSource(AudioSource audioSource, float volume)
    {
        audioSource.playOnAwake = true;
        audioSource.volume = volume;
        audioSource.pitch = 1f;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private static Transform CreateChildTransform(string name, Transform parent, Vector3 localPosition, int layer)
    {
        GameObject child = new GameObject(name);
        child.layer = layer;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }

    private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void AddUrpCameraDataIfAvailable(GameObject cameraObject)
    {
        Type urpCameraDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpCameraDataType == null)
        {
            return;
        }

        if (cameraObject.GetComponent(urpCameraDataType) == null)
        {
            cameraObject.AddComponent(urpCameraDataType);
        }
    }

    private static int GetLayerOrFallback(string layerName, int fallback)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? layer : fallback;
    }

    private static bool TagExists(string tag)
    {
        string[] tags = InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tag)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static bool HasRequiredPlayerSystems(GameObject prefabRoot)
    {
        if (prefabRoot == null)
        {
            return false;
        }

        PlayerMovement movement = prefabRoot.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            return false;
        }

        if (movement.vfxPrefab == null || movement.sonarSound == null)
        {
            return false;
        }

        Transform playerCam = prefabRoot.transform.Find("CameraHolder/PlayerCam");
        if (playerCam == null || playerCam.GetComponent<Camera>() == null)
        {
            return false;
        }

        if (movement.sonarCooldownFillImage == null || movement.sonarCooldownBackgroundImage == null || movement.sonarKeyLabel == null)
        {
            return false;
        }

        return true;
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FlashlightPrefabBuilder
{
    private const string PrefabDirectory = "Assets/Prefab";
    private const string PrefabPath = PrefabDirectory + "/FlashlightPickup.prefab";

    [MenuItem("Tools/Prefabs/Create Flashlight Pickup Prefab")]
    public static void CreateOrUpdateFlashlightPrefabFromMenu()
    {
        CreateOrUpdateFlashlightPrefab(logResult: true);
    }

    [InitializeOnLoadMethod]
    private static void EnsureFlashlightPrefabExists()
    {
        EditorApplication.delayCall += TryAutoCreatePrefab;
    }

    private static void TryAutoCreatePrefab()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            return;
        }

        CreateOrUpdateFlashlightPrefab(logResult: false);
    }

    private static void CreateOrUpdateFlashlightPrefab(bool logResult)
    {
        EnsureFolderExists(PrefabDirectory);

        GameObject root = new GameObject("FlashlightPickup");
        try
        {
            BuildPrefab(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logResult)
            {
                if (success)
                {
                    Debug.Log($"[FLASHLIGHT PREFAB] Created/Updated: {PrefabPath}");
                }
                else
                {
                    Debug.LogError($"[FLASHLIGHT PREFAB] Failed to create/update: {PrefabPath}");
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildPrefab(GameObject root)
    {
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, 0.09f);
        collider.size = new Vector3(0.22f, 0.22f, 0.5f);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.6f;
        rb.drag = 0.15f;
        rb.angularDrag = 0.2f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        FlashlightPickup pickup = root.AddComponent<FlashlightPickup>();
        pickup.pickupKey = KeyCode.F;
        pickup.toggleFlashlightKey = KeyCode.Tab;
        pickup.pickupDistance = 2.2f;
        pickup.heldLocalPosition = new Vector3(-0.047f, -0.014f, -0.174f);
        pickup.heldLocalEulerAngles = new Vector3(-7.956f, 29.912f, -0.372f);
        pickup.heldLocalScale = Vector3.one;
        pickup.flashlightColor = new Color(1f, 0.97f, 0.9f, 1f);
        pickup.flashlightEmission = 6f;
        pickup.flashlightRange = 18f;
        pickup.flashlightSpotAngle = 58f;
        pickup.flashlightInnerSpotAngle = 32f;
        pickup.lightForwardOffset = 0.08f;
        pickup.decayDurationSeconds = 5f;
        pickup.clickRechargePercent = 5f;
        pickup.decayCurvePower = 2f;
        pickup.decayDirection = FlashlightPickup.DecayDirection.SlowToFast;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
        RemoveCollider(body);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        head.transform.localRotation = Quaternion.identity;
        head.transform.localScale = new Vector3(0.14f, 0.14f, 0.08f);
        RemoveCollider(head);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tail.name = "Tail";
        tail.transform.SetParent(root.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0f, -0.22f);
        tail.transform.localRotation = Quaternion.identity;
        tail.transform.localScale = new Vector3(0.1f, 0.1f, 0.08f);
        RemoveCollider(tail);

        GameObject lightObject = new GameObject("FlashlightLight");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0f, 0.22f);
        lightObject.transform.localRotation = Quaternion.identity;

        Light flashlightLight = lightObject.AddComponent<Light>();
        flashlightLight.type = LightType.Spot;
        flashlightLight.color = new Color(1f, 0.97f, 0.9f, 1f);
        flashlightLight.intensity = 6f;
        flashlightLight.range = 18f;
        flashlightLight.spotAngle = 58f;
        flashlightLight.innerSpotAngle = 32f;
        flashlightLight.cullingMask = ~0;
        flashlightLight.renderMode = LightRenderMode.Auto;
        flashlightLight.shadows = LightShadows.Soft;
        flashlightLight.enabled = true;
    }

    private static void RemoveCollider(GameObject primitiveObject)
    {
        Collider primitiveCollider = primitiveObject.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            Object.DestroyImmediate(primitiveCollider);
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
}
#endif

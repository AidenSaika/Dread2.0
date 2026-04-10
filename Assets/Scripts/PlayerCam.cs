using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Look Settings")]
    public float sensX;
    public float sensY;

    public Transform orientation;

    [Header("Head Light")]
    [SerializeField] private bool autoCreateHeadLight = true;
    [SerializeField] private string headLightObjectName = "PlayerHeadLight";
    [SerializeField] private Light headLight;
    [SerializeField, Min(0.1f)] private float headLightRange = 2f;
    [SerializeField, Min(0f)] private float headLightIntensity = 0.05f;
    [SerializeField] private Color headLightColor = Color.white;

    float xRotation;
    float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        headLightRange = 2f;
        headLightIntensity = 0.05f;
        SetupHeadLight();
    }

    private void Update()
    {
        // get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // rotate cam and orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void SetupHeadLight()
    {
        if (!autoCreateHeadLight && headLight == null)
        {
            return;
        }

        if (headLight == null)
        {
            Transform headLightTransform = transform.Find(headLightObjectName);
            if (headLightTransform == null)
            {
                GameObject headLightObject = new GameObject(headLightObjectName);
                headLightTransform = headLightObject.transform;
                headLightTransform.SetParent(transform, false);
                headLightTransform.localPosition = Vector3.zero;
                headLightTransform.localRotation = Quaternion.identity;
            }

            headLight = headLightTransform.GetComponent<Light>();
            if (headLight == null)
            {
                headLight = headLightTransform.gameObject.AddComponent<Light>();
            }
        }

        if (headLight == null)
        {
            return;
        }

        Transform lightTransform = headLight.transform;
        if (lightTransform.parent != transform)
        {
            lightTransform.SetParent(transform, false);
        }
        lightTransform.localPosition = Vector3.zero;
        lightTransform.localRotation = Quaternion.identity;

        headLight.type = LightType.Point;
        headLight.range = headLightRange;
        headLight.intensity = headLightIntensity;
        headLight.color = headLightColor;
        headLight.shadows = LightShadows.None;
        headLight.enabled = true;
    }
}

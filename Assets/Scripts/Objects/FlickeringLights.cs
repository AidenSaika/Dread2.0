using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickeringLights : MonoBehaviour
{
    public Light[] lights; 
    public float minIntensity = 0.5f; 
    public float maxIntensity = 2f; 
    public float flickerSpeed = 1f; 

    void Start()
    {
        // Start the flickering effect for each light
        foreach (Light light in lights)
        {
            StartCoroutine(FlickerLight(light));
        }
    }

    IEnumerator FlickerLight(Light light)
    {
        while (true)  // Keep running the flickering effect indefinitely
        {
            float flickerDuration = Random.Range(0.1f, 0.5f);
            float targetIntensity = Mathf.PingPong(Time.time * flickerSpeed, maxIntensity - minIntensity) + minIntensity;

            light.intensity = targetIntensity;
            yield return new WaitForSeconds(flickerDuration);
        }
    }
}
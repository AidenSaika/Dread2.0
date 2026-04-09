using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndSceneTrigger : MonoBehaviour
{
    public GameObject player;
    public CanvasGroup whiteScreenOverlay;  // UI Canvas with an Image that will turn white
    public float fadeDuration = 3f;         // Duration for the screen to fade to white
    public float soundFadeDuration = 3f;    // Duration for the sound to fade out

    private bool playerInEndZone = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            if (!playerInEndZone)
            {
                playerInEndZone = true;
                StartCoroutine(EndGameSequence());
            }
        }
    }

    IEnumerator EndGameSequence()
    {
        // Start fading the screen to white
        StartCoroutine(FadeToWhite());

        // Start fading out the sound
        StartCoroutine(FadeOutSound());

        // Wait for both the fade and sound to finish before any further actions
        yield return new WaitForSeconds(fadeDuration);

        // You can then load an ending scene or display the final UI, credits, etc.
        // For example: SceneManager.LoadScene("EndingScene");
    }

    IEnumerator FadeToWhite()
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            // Gradually increase the alpha value of the white overlay
            whiteScreenOverlay.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }
        whiteScreenOverlay.alpha = 1f; // Ensure fully white at the end
    }

    IEnumerator FadeOutSound()
    {
        float startVolume = AudioListener.volume;
        float time = 0f;
        while (time < soundFadeDuration)
        {
            time += Time.deltaTime;
            // Gradually reduce the global audio volume
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, time / soundFadeDuration);
            yield return null;
        }
        AudioListener.volume = 0f; // Ensure sound is fully muted at the end
    }
}
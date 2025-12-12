using UnityEngine;
using System.Collections;

public class OrbTransitionManager : MonoBehaviour
{
    [Header("Orb Settings")]
    [Tooltip("The orb prefab to instantiate (should be a Sphere GameObject)")]
    public GameObject orbPrefab;

    [Header("Transition Settings")]
    [Tooltip("Distance threshold from orb to face to trigger transition (in meters)")]
    public float faceTriggerDistance = 0.15f;

    [Tooltip("Duration of the scale animation when orb appears (in seconds)")]
    public float orbScaleDuration = 0.2f;

    [Tooltip("Duration of the white fade transition (in seconds)")]
    public float transitionFadeDuration = 0.5f;

    [Header("Audio")]
    [Tooltip("Sound to play when transition is triggered")]
    public AudioClip teleportSound;

    private Transform leftHandAnchor;
    private Transform centerEyeAnchor;
    private GameObject currentOrb;
    private AudioSource audioSource;
    private bool isButtonHeld = false;
    private bool isTransitioning = false;
    private bool hasTransitionedThisPress = false; // Track if we've already transitioned on this button press
    private Coroutine scaleCoroutine;
    private Vector3 prefabOriginalScale;

    void Start()
    {
        // Find OVRCameraRig to get hand and eye anchors
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {
            leftHandAnchor = cameraRig.leftHandAnchor;
            centerEyeAnchor = cameraRig.centerEyeAnchor;
        }
        else
        {
            Debug.LogError("OrbTransitionManager: Could not find OVRCameraRig! Make sure it's in the scene.");
        }

        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // Warn if orb prefab is not assigned
        if (orbPrefab == null)
        {
            Debug.LogWarning("OrbTransitionManager: Orb prefab is not assigned! Please assign it in the Inspector.");
        }
    }

    void Update()
    {
        // Don't allow orb if game hasn't started yet
        if (ScavengerGameManager.Instance != null && !ScavengerGameManager.Instance.IsGameStarted())
        {
            // If orb is active, destroy it
            if (currentOrb != null)
            {
                DestroyOrb();
            }
            return;
        }

        // Don't allow orb if game is completed (victory screen showing)
        if (ScavengerGameManager.Instance != null && ScavengerGameManager.Instance.IsGameCompleted())
        {
            // If orb is active, destroy it
            if (currentOrb != null)
            {
                DestroyOrb();
            }
            return;
        }

        // Check if Button X (Button.Three) is being held
        bool buttonPressed = OVRInput.Get(OVRInput.Button.Three);

        // Button just pressed - show orb (only if we haven't transitioned on this press and not transitioning)
        if (buttonPressed && !isButtonHeld && currentOrb == null && !hasTransitionedThisPress && !isTransitioning)
        {
            ShowOrb();
            isButtonHeld = true;
            hasTransitionedThisPress = false; // Reset flag when starting new press
            Debug.Log("OrbTransitionManager: Button pressed - showing orb");
        }
        // Button just released - hide orb
        else if (!buttonPressed && isButtonHeld)
        {
            // Only destroy orb if we're not transitioning (transition might have already destroyed it)
            if (currentOrb != null)
            {
                DestroyOrb();
            }
            isButtonHeld = false;
            Debug.Log("OrbTransitionManager: Button released - hiding orb");
        }

        // Update orb position and rotation to follow hand (only if orb exists and we haven't transitioned)
        if (currentOrb != null && leftHandAnchor != null && !hasTransitionedThisPress)
        {
            currentOrb.transform.position = leftHandAnchor.position;
            currentOrb.transform.rotation = leftHandAnchor.rotation;

            // Check distance to face
            if (centerEyeAnchor != null)
            {
                float distance = Vector3.Distance(currentOrb.transform.position, centerEyeAnchor.position);
                
                if (distance < faceTriggerDistance)
                {
                    // Trigger transition!
                    TriggerTransition();
                }
            }
        }
    }

    void ShowOrb()
    {
        if (orbPrefab == null || leftHandAnchor == null)
        {
            return;
        }

        // Store the prefab's original scale (so we respect the prefab's size)
        prefabOriginalScale = orbPrefab.transform.localScale;

        // Instantiate orb at hand position
        currentOrb = Instantiate(orbPrefab, leftHandAnchor.position, leftHandAnchor.rotation);
        currentOrb.name = "TransitionOrb";

        // Start scale animation from 0 to prefab's original scale
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleOrbAnimation(Vector3.zero, prefabOriginalScale, orbScaleDuration));

        Debug.Log("OrbTransitionManager: Orb appeared at hand position");
    }

    void DestroyOrb()
    {
        if (currentOrb != null)
        {
            // Stop scale coroutine if running
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
                scaleCoroutine = null;
            }

            Destroy(currentOrb);
            currentOrb = null;

            Debug.Log("OrbTransitionManager: Orb destroyed");
        }
    }

    void TriggerTransition()
    {
        if (isTransitioning || RealityManager.Instance == null || hasTransitionedThisPress)
        {
            Debug.LogWarning($"OrbTransitionManager: Cannot trigger transition - isTransitioning: {isTransitioning}, hasTransitionedThisPress: {hasTransitionedThisPress}");
            return;
        }

        // Mark that we've transitioned on this button press
        hasTransitionedThisPress = true;
        isTransitioning = true;

        Debug.Log("OrbTransitionManager: Orb touched face! Triggering white fade transition...");

        // Play teleport sound
        if (audioSource != null && teleportSound != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }

        // Determine which world to switch to
        bool switchToVR = !RealityManager.Instance.IsInVR;
        Debug.Log($"OrbTransitionManager: Switching to {(switchToVR ? "VR" : "AR")}");

        // Immediately hide orb (so it doesn't trigger multiple times)
        DestroyOrb();
        isButtonHeld = false; // Reset button state so user must release and press again

        // Start white fade transition coroutine
        StartCoroutine(WhiteFadeTransition(switchToVR));
    }

    IEnumerator WhiteFadeTransition(bool switchToVR)
    {
        // Use RealityManager's fade transition with white color
        if (RealityManager.Instance != null)
        {
            // Temporarily store original fade duration and set it to our transition duration
            float originalFadeDuration = RealityManager.Instance.fadeDuration;
            RealityManager.Instance.fadeDuration = transitionFadeDuration;
            
            RealityManager.Instance.StartFadeTransition(switchToVR, Color.white);
            
            // Wait for the transition to complete (fadeDuration * 2 + 0.1f wait time)
            yield return new WaitForSeconds(transitionFadeDuration * 2f + 0.1f);
            
            // Restore original fade duration
            RealityManager.Instance.fadeDuration = originalFadeDuration;
        }
        else
        {
            // Fallback if RealityManager is null
            yield return new WaitForSeconds(transitionFadeDuration * 2f + 0.1f);
        }

        // Reset transition flags after transition completes
        isTransitioning = false;
        hasTransitionedThisPress = false; // Reset this flag so orb can be used again
        
        Debug.Log("OrbTransitionManager: White fade transition complete - ready for next use");
    }

    IEnumerator ScaleOrbAnimation(Vector3 startScale, Vector3 endScale, float duration)
    {
        if (currentOrb == null)
        {
            yield break;
        }

        // Set initial scale
        currentOrb.transform.localScale = startScale;

        float elapsed = 0f;

        while (elapsed < duration && currentOrb != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth interpolation (ease out)
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease out
            
            currentOrb.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }

        // Ensure final scale is set to the prefab's original scale
        if (currentOrb != null)
        {
            currentOrb.transform.localScale = endScale;
        }

        scaleCoroutine = null;
    }
}


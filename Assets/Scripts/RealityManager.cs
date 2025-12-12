using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RealityManager : MonoBehaviour
{
    public static RealityManager Instance { get; private set; }

    [Header("References")]
    public OVRPassthroughLayer passthrough;
    public GameObject vrEnvironmentRoot;
    public Camera targetCamera;

    [Header("Alignment Settings")]
    [Tooltip("Y offset to align VR environment with AR passthrough floor. Adjust if VR appears lower than AR.")]
    public float vrEnvironmentYOffset = 0f;

    [Header("Fade Transition Settings")]
    [Tooltip("Duration of fade in/out in seconds")]
    public float fadeDuration = 0.5f;
    [Tooltip("Color of the fade overlay")]
    public Color fadeColor = Color.black;

    private bool isInVR = false;
    private Vector3 originalVREnvironmentPosition;
    private OVRManager ovrManager;
    private GameObject fadeQuad;
    private Material fadeMaterial;
    private MeshRenderer fadeRenderer;
    private bool isTransitioning = false;

    // Public property to check current world state
    public bool IsInVR => isInVR;
    public bool IsInAR => !isInVR;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Get OVRManager to ensure tracking origin is set correctly
        ovrManager = FindFirstObjectByType<OVRManager>();
        
        // Ensure tracking origin is set to Floor level for proper alignment
        if (ovrManager != null)
        {
            // Set tracking origin to Floor to align with passthrough floor
            ovrManager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
        }

        // Store original VR environment position
        if (vrEnvironmentRoot != null)
        {
            originalVREnvironmentPosition = vrEnvironmentRoot.transform.position;
        }

        // Setup fade overlay
        SetupFadeOverlay();

        // Initialize to AR mode by default
        SwitchToAR();
    }

    void Update()
    {
        // Don't allow transitions if already transitioning
        if (isTransitioning)
        {
            return;
        }

        // Cut transition when A button (Button.One) is pressed
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (isInVR)
            {
                SwitchToAR();
            }
            else
            {
                SwitchToVR();
            }
        }

        // Fade transition when B button (Button.Two) is pressed
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            if (isInVR)
            {
                StartFadeTransition(false); // Switch to AR
            }
            else
            {
                StartFadeTransition(true); // Switch to VR
            }
        }
    }

    public void SwitchToVR()
    {
        isInVR = true;

        // Disable passthrough
        if (passthrough != null)
        {
            passthrough.enabled = false;
        }

        // Enable VR environment and adjust position to align with AR floor
        if (vrEnvironmentRoot != null)
        {
            vrEnvironmentRoot.SetActive(true);
            
            // Apply Y offset to align VR environment with AR passthrough floor
            Vector3 adjustedPosition = originalVREnvironmentPosition;
            adjustedPosition.y += vrEnvironmentYOffset;
            vrEnvironmentRoot.transform.position = adjustedPosition;
        }

        // Set camera clear flags to Skybox
        if (targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.Skybox;
        }

        // Notify ScavengerGameManager of world change
        NotifyWorldChanged();
    }

    public void SwitchToAR()
    {
        isInVR = false;

        // Enable passthrough
        if (passthrough != null)
        {
            passthrough.enabled = true;
        }

        // Disable VR environment and restore original position
        if (vrEnvironmentRoot != null)
        {
            vrEnvironmentRoot.SetActive(false);
            // Restore original position when disabled
            vrEnvironmentRoot.transform.position = originalVREnvironmentPosition;
        }

        // Set camera clear flags to SolidColor (Black, Alpha 0)
        if (targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = new Color(0, 0, 0, 0);
        }

        // Notify ScavengerGameManager of world change
        NotifyWorldChanged();
    }

    void NotifyWorldChanged()
    {
        // Notify ScavengerGameManager if it exists
        ScavengerGameManager gameManager = FindFirstObjectByType<ScavengerGameManager>();
        if (gameManager != null)
        {
            gameManager.OnWorldChanged(isInVR);
        }
    }

    void SetupFadeOverlay()
    {
        // Ensure we have a target camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }
        }

        // Create fade quad as a child of the camera itself
        fadeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fadeQuad.name = "FadeQuad";
        fadeQuad.transform.SetParent(targetCamera.transform, false);
        
        // Position quad in front of camera (local space)
        fadeQuad.transform.localPosition = new Vector3(0, 0, 0.5f); // 50cm in front
        fadeQuad.transform.localRotation = Quaternion.identity;
        fadeQuad.transform.localScale = new Vector3(5f, 5f, 1f); // Large enough to cover view
        
        // Remove collider (we don't need it)
        Collider collider = fadeQuad.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // Try to use Oculus shader, fallback to standard transparent shader
        Shader fadeShader = Shader.Find("Oculus/Unlit Transparent Color");
        if (fadeShader == null)
        {
            fadeShader = Shader.Find("Unlit/Transparent");
        }
        if (fadeShader == null)
        {
            fadeShader = Shader.Find("Standard");
        }
        
        if (fadeShader == null)
        {
            Debug.LogError("RealityManager: Could not find a suitable shader for fade overlay!");
            return;
        }
        
        Debug.Log($"RealityManager: Using shader '{fadeShader.name}' for fade overlay");
        
        // Create material for fade
        fadeMaterial = new Material(fadeShader);
        
        // Set color with proper alpha
        Color initialColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeMaterial.color = initialColor;
        
        // If using Standard shader, set rendering mode to transparent
        if (fadeShader.name == "Standard")
        {
            fadeMaterial.SetFloat("_Mode", 3); // Transparent mode
            fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fadeMaterial.SetInt("_ZWrite", 0);
            fadeMaterial.DisableKeyword("_ALPHATEST_ON");
            fadeMaterial.EnableKeyword("_ALPHABLEND_ON");
            fadeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            fadeMaterial.renderQueue = 3000;
        }
        else
        {
            fadeMaterial.renderQueue = 4000; // Transparent queue
        }
        
        // Apply material to renderer
        fadeRenderer = fadeQuad.GetComponent<MeshRenderer>();
        fadeRenderer.material = fadeMaterial;
        fadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fadeRenderer.receiveShadows = false;
        
        // Initially disable
        fadeQuad.SetActive(false);
    }

    public void StartFadeTransition(bool switchToVR)
    {
        if (isTransitioning)
        {
            return; // Already transitioning
        }

        StartCoroutine(FadeTransition(switchToVR));
    }

    IEnumerator FadeTransition(bool switchToVR)
    {
        isTransitioning = true;

        Debug.Log($"RealityManager: Starting fade transition to {(switchToVR ? "VR" : "AR")}");

        // Fade to black
        Debug.Log("RealityManager: Fading to black...");
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        Debug.Log("RealityManager: Faded to black, switching worlds...");

        // Switch worlds while black
        if (switchToVR)
        {
            SwitchToVR();
        }
        else
        {
            SwitchToAR();
        }

        // Wait a brief moment at full black
        yield return new WaitForSeconds(0.1f);

        // Fade back in
        Debug.Log("RealityManager: Fading back in...");
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        Debug.Log("RealityManager: Fade transition complete");

        isTransitioning = false;
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (fadeQuad == null || fadeMaterial == null)
        {
            Debug.LogWarning("RealityManager: FadeQuad or FadeMaterial is null!");
            yield break;
        }

        // Enable quad if fading in (alpha > 0)
        if (endAlpha > 0 || startAlpha > 0)
        {
            fadeQuad.SetActive(true);
            fadeRenderer.enabled = true;
        }

        float elapsed = 0f;
        Color color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, startAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            // Update color with proper RGB values from fadeColor
            color.r = fadeColor.r;
            color.g = fadeColor.g;
            color.b = fadeColor.b;
            color.a = alpha;
            
            fadeMaterial.color = color;
            
            // Force material update
            fadeRenderer.material = fadeMaterial;
            
            yield return null;
        }

        // Ensure final alpha is set
        color.a = endAlpha;
        fadeMaterial.color = color;
        fadeRenderer.material = fadeMaterial;
        
        // Disable quad if fully transparent
        if (endAlpha <= 0)
        {
            fadeQuad.SetActive(false);
        }
    }
}

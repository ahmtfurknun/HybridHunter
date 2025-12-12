using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserPointerInteractor : MonoBehaviour
{
    [Header("Laser Settings")]
    public float maxLaserDistance = 5f;
    public Color laserColor = Color.red;
    public float laserWidth = 0.01f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip laserShotSound;

    private LineRenderer lineRenderer;
    private RaycastHit hitInfo;
    private bool isHittingCollectible = false;
    private GameObject currentHitObject = null;

    void Start()
    {
        // Get or add LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = laserColor;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    void Update()
    {
        // Don't show laser or allow interaction if game hasn't started
        if (ScavengerGameManager.Instance == null || !ScavengerGameManager.Instance.IsGameStarted())
        {
            // Hide laser
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            return;
        }

        // Show laser
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        // Cast ray from controller position forward
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;

        // Perform raycast
        bool hit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, maxLaserDistance);

        // Update laser visual
        UpdateLaserVisual(hit);

        // Check if hitting a collectible
        CheckCollectibleHit(hit);

        // Check for trigger input (but only for collectibles/chest, not for starting game)
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            OnTriggerPressed();
        }
    }

    void UpdateLaserVisual(bool hit)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Vector3 startPoint = transform.position;
        Vector3 endPoint;

        if (hit)
        {
            // Stop laser at hit point
            endPoint = hitInfo.point;
        }
        else
        {
            // Extend laser to max distance
            endPoint = transform.position + transform.forward * maxLaserDistance;
        }

        // Update line renderer positions
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    void CheckCollectibleHit(bool hit)
    {
        isHittingCollectible = false;
        currentHitObject = null;

        if (hit && hitInfo.collider != null)
        {
            GameObject hitObject = hitInfo.collider.gameObject;

            // Check if the hit object or its parent has the "Collectible" tag
            if (hitObject.CompareTag("Collectible"))
            {
                isHittingCollectible = true;
                currentHitObject = hitObject;
            }
            else if (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("Collectible"))
            {
                isHittingCollectible = true;
                currentHitObject = hitObject.transform.parent.gameObject;
            }
            // Also check if any parent up the hierarchy has the tag
            else
            {
                Transform parent = hitObject.transform.parent;
                while (parent != null)
                {
                    if (parent.CompareTag("Collectible"))
                    {
                        isHittingCollectible = true;
                        currentHitObject = parent.gameObject;
                        break;
                    }
                    parent = parent.parent;
                }
            }

            // Also check for Chest interaction
            if (!isHittingCollectible)
            {
                if (hitObject.CompareTag("Chest") || 
                    (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("Chest")))
                {
                    GameObject chestObject = hitObject.CompareTag("Chest") ? hitObject : hitObject.transform.parent.gameObject;
                    ChestInteractor chest = chestObject.GetComponent<ChestInteractor>();
                    if (chest != null)
                    {
                        isHittingCollectible = true; // Use same flag for interaction
                        currentHitObject = chestObject;
                    }
                }
            }
        }
    }

    void OnTriggerPressed()
    {
        Debug.Log($"LaserPointerInteractor: Trigger pressed. isHittingCollectible: {isHittingCollectible}, currentHitObject: {(currentHitObject != null ? currentHitObject.name : "null")}");

        // Only interact if hitting a collectible or chest
        if (isHittingCollectible && currentHitObject != null)
        {
            // Check if it's a chest
            if (currentHitObject.CompareTag("Chest"))
            {
                Debug.Log($"LaserPointerInteractor: Attempting to open chest {currentHitObject.name}");

                // Play laser shot sound
                if (audioSource != null && laserShotSound != null)
                {
                    audioSource.PlayOneShot(laserShotSound);
                }

                // Interact with chest
                ChestInteractor chest = currentHitObject.GetComponent<ChestInteractor>();
                if (chest != null)
                {
                    chest.OnPlayerInteract();
                }
            }
            else
            {
                // It's a collectible
                Debug.Log($"LaserPointerInteractor: Attempting to collect {currentHitObject.name}");

                // Play laser shot sound
                if (audioSource != null && laserShotSound != null)
                {
                    audioSource.PlayOneShot(laserShotSound);
                }

                // Notify ScavengerGameManager
                if (ScavengerGameManager.Instance != null)
                {
                    ScavengerGameManager.Instance.ObjectFound(currentHitObject);
                }
                else
                {
                    Debug.LogWarning("LaserPointerInteractor: ScavengerGameManager.Instance is null!");
                }
            }
        }
        else
        {
            Debug.Log($"LaserPointerInteractor: Cannot interact - isHittingCollectible: {isHittingCollectible}, currentHitObject: {(currentHitObject != null ? currentHitObject.name : "null")}");
        }
    }
}


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
        // Cast ray from controller position forward
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;

        // Perform raycast
        bool hit = Physics.Raycast(rayOrigin, rayDirection, out hitInfo, maxLaserDistance);

        // Update laser visual
        UpdateLaserVisual(hit);

        // Check if hitting a collectible
        CheckCollectibleHit(hit);

        // Check for trigger input
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
            if (hitObject.CompareTag("Collectible") || 
                (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("Collectible")))
            {
                isHittingCollectible = true;
                currentHitObject = hitObject.CompareTag("Collectible") ? hitObject : hitObject.transform.parent.gameObject;
            }
        }
    }

    void OnTriggerPressed()
    {
        // Only interact if hitting a collectible
        if (isHittingCollectible && currentHitObject != null)
        {
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
}


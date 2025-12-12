using UnityEngine;

public class ChestInteractor : MonoBehaviour
{
    [Header("Settings")]
    public string chestTag = "Chest";
    public AudioSource audioSource;
    public AudioClip chestOpenSound;

    private bool allItemsCollected = false;

    void Start()
    {
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ensure this object has the Chest tag
        if (!gameObject.CompareTag(chestTag))
        {
            Debug.LogWarning($"ChestInteractor: GameObject {gameObject.name} should have tag '{chestTag}'!");
        }
    }

    void Update()
    {
        // Check if all items are collected
        if (ScavengerGameManager.Instance != null)
        {
            allItemsCollected = ScavengerGameManager.Instance.AreAllItemsCollected();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered trigger
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            OnPlayerInteract();
        }
    }

    public void OnPlayerInteract()
    {
        // Only allow interaction if all items are collected
        if (!allItemsCollected)
        {
            Debug.Log("ChestInteractor: Cannot open chest - not all items collected yet!");
            
            // Show feedback message
            if (ScavengerGameManager.Instance != null)
            {
                ScavengerGameManager.Instance.ShowFeedback("You need to collect all items first!", 2.0f);
            }
            return;
        }

        Debug.Log("ChestInteractor: Player opened the chest!");

        // Play chest open sound
        if (audioSource != null && chestOpenSound != null)
        {
            audioSource.PlayOneShot(chestOpenSound);
        }

        // Notify ScavengerGameManager to show victory
        if (ScavengerGameManager.Instance != null)
        {
            ScavengerGameManager.Instance.OnChestOpened();
        }
    }
}


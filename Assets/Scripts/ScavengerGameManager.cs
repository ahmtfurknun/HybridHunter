using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScavengerGameManager : MonoBehaviour
{
    public static ScavengerGameManager Instance { get; private set; }

    [Header("Collectibles")]
    public List<GameObject> items;

    [Header("Settings")]
    public float interactionDistance = 2f;
    public AudioSource audioSource;
    public AudioClip collectSound;

    [Header("Victory")]
    public GameObject victoryTextObject;
    public Text victoryText;

    private int currentItemIndex = 0;
    private Camera playerCamera;
    private bool[] itemWorldTypes; // true = VR, false = AR

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
        // Get player camera (CenterEyeAnchor)
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            // Try to find the camera in the scene
            playerCamera = FindFirstObjectByType<Camera>();
        }

        // Validate items list
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("ScavengerGameManager: No items assigned in the list!");
            return;
        }

        // Setup victory text
        SetupVictoryText();

        // Initialize world types for items (alternating: AR, VR, AR, VR...)
        // Item 0 = AR, Item 1 = VR, Item 2 = AR, etc.
        itemWorldTypes = new bool[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            itemWorldTypes[i] = (i % 2 == 1); // Odd indices = VR, Even indices = AR
        }

        // Hide all items first
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetActive(false);
            }
        }

        currentItemIndex = 0;

        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Update visibility based on current world (only if RealityManager is ready)
        if (RealityManager.Instance != null)
        {
            UpdateItemVisibility();
        }
    }

    void SetupVictoryText()
    {
        // Create victory text if not assigned
        if (victoryTextObject == null)
        {
            // Try to find existing victory text
            GameObject existingText = GameObject.Find("VictoryText");
            if (existingText == null)
            {
                // Create Canvas for victory text
                GameObject canvasObj = new GameObject("VictoryCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10000;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();

                // Create Text object
                victoryTextObject = new GameObject("VictoryText");
                victoryTextObject.transform.SetParent(canvasObj.transform, false);

                RectTransform rectTransform = victoryTextObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(800, 200);
                rectTransform.anchoredPosition = Vector2.zero;

                victoryText = victoryTextObject.AddComponent<Text>();
                victoryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                victoryText.fontSize = 72;
                victoryText.alignment = TextAnchor.MiddleCenter;
                victoryText.color = Color.yellow;
                victoryText.text = "VICTORY!";
            }
            else
            {
                victoryTextObject = existingText;
                victoryText = existingText.GetComponent<Text>();
            }
        }

        // Hide victory text initially
        if (victoryTextObject != null)
        {
            victoryTextObject.SetActive(false);
        }
    }

    public void OnWorldChanged(bool isInVR)
    {
        // Update item visibility when world changes
        UpdateItemVisibility();
    }

    void UpdateItemVisibility()
    {
        // Early return if not initialized yet
        if (itemWorldTypes == null || items == null || items.Count == 0)
        {
            return;
        }

        if (RealityManager.Instance == null)
        {
            return;
        }

        bool isInVR = RealityManager.Instance.IsInVR;

        // Hide all items first
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetActive(false);
            }
        }

        // Show current item only if we're in the correct world
        if (currentItemIndex >= 0 && currentItemIndex < items.Count && 
            currentItemIndex < itemWorldTypes.Length && 
            items[currentItemIndex] != null)
        {
            bool currentItemIsVR = itemWorldTypes[currentItemIndex];
            if (currentItemIsVR == isInVR)
            {
                items[currentItemIndex].SetActive(true);
            }
        }
    }


    public void ObjectFound(GameObject item)
    {
        // Early return if not initialized
        if (itemWorldTypes == null || items == null || items.Count == 0)
        {
            return;
        }

        // Check if the found item is the current item we're looking for
        if (currentItemIndex >= items.Count || items[currentItemIndex] != item)
        {
            // Not the current item, ignore
            return;
        }

        // Only allow interaction if we're in the correct world for the current item
        bool currentItemIsVR = itemWorldTypes[currentItemIndex];
        bool isInVR = RealityManager.Instance != null && RealityManager.Instance.IsInVR;

        if (currentItemIsVR != isInVR)
        {
            // Wrong world, can't collect
            return;
        }

        // Play success sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }

        // Disable the found item
        if (item != null)
        {
            item.SetActive(false);
        }

        // Move to next item
        currentItemIndex++;

        if (currentItemIndex >= items.Count || currentItemIndex >= itemWorldTypes.Length)
        {
            // All items found - Show Victory
            ShowVictory();
        }
        else
        {
            // Update visibility for the next item (will show when player switches to correct world)
            UpdateItemVisibility();
        }
    }

    void ShowVictory()
    {
        Debug.Log("Victory! All items collected!");

        // Show victory text
        if (victoryTextObject != null)
        {
            victoryTextObject.SetActive(true);
        }
        else if (victoryText != null)
        {
            victoryText.gameObject.SetActive(true);
        }
    }
}

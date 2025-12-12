using System.Collections;
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
    [TextArea(3, 10)]
    public string victoryMessage = "Victory! You opened the chest and got the treasure.\n\nTo replay, press left trigger.";

    [Header("Feedback")]
    public GameObject feedbackTextObject;
    public Text feedbackText;

    private int currentItemIndex = 0;
    private Camera playerCamera;
    private bool[] itemWorldTypes; // true = VR, false = AR
    private bool gameStarted = false;
    private bool allItemsCollected = false;
    private bool gameCompleted = false;

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

        // Setup feedback text
        SetupFeedbackText();

        // Initialize world types for items (alternating: AR, VR, AR, VR...)
        // Item 0 = AR, Item 1 = VR, Item 2 = AR, etc.
        itemWorldTypes = new bool[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            itemWorldTypes[i] = (i % 2 == 1); // Odd indices = VR, Even indices = AR
        }

        // Hide all items first (game hasn't started yet)
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetActive(false);
            }
        }

        currentItemIndex = 0;
        gameStarted = false;
        allItemsCollected = false;

        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Don't show items until game starts
        // Update visibility will be called when game starts
        
        Debug.Log("ScavengerGameManager: Initialized. Waiting for game start...");
    }

    public void StartGame()
    {
        if (gameStarted)
        {
            return;
        }

        gameStarted = true;
        Debug.Log("ScavengerGameManager: Game started!");

        // Update visibility based on current world (only if RealityManager is ready)
        if (RealityManager.Instance != null)
        {
            UpdateItemVisibility();
        }
    }

    public bool AreAllItemsCollected()
    {
        return allItemsCollected;
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }

    public bool IsGameCompleted()
    {
        return gameCompleted;
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
                // Get the camera (for VR compatibility)
                Camera targetCamera = playerCamera;
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                    if (targetCamera == null)
                    {
                        targetCamera = FindFirstObjectByType<Camera>();
                    }
                }

                if (targetCamera == null)
                {
                    Debug.LogError("ScavengerGameManager: No camera found! Cannot create victory text.");
                    return;
                }

                // Create Canvas for victory text
                GameObject canvasObj = new GameObject("VictoryCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                
                // Use Screen Space - Camera mode for VR compatibility (same as welcome UI)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = targetCamera;
                canvas.planeDistance = 0.5f; // Same distance as welcome UI
                canvas.sortingOrder = 10000; // Below welcome UI

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                // Create background panel
                GameObject panelObj = new GameObject("VictoryBackground");
                panelObj.transform.SetParent(canvasObj.transform, false);
                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black

                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;

                // Create Text object
                victoryTextObject = new GameObject("VictoryText");
                victoryTextObject.transform.SetParent(canvasObj.transform, false);

                RectTransform rectTransform = victoryTextObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(800, 200); // Similar size to welcome title
                rectTransform.anchoredPosition = Vector2.zero;

                victoryText = victoryTextObject.AddComponent<Text>();
                victoryText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                victoryText.fontSize = 24; // Same as welcome title
                victoryText.fontStyle = FontStyle.Bold;
                victoryText.alignment = TextAnchor.MiddleCenter;
                victoryText.color = Color.yellow;
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

        // Set initial victory text
        if (victoryText != null)
        {
            victoryText.text = victoryMessage;
        }
    }

    void SetupFeedbackText()
    {
        // Create feedback text if not assigned
        if (feedbackTextObject == null)
        {
            // Try to find existing feedback text
            GameObject existingText = GameObject.Find("FeedbackText");
            if (existingText == null)
            {
                // Get the camera (for VR compatibility)
                Camera targetCamera = playerCamera;
                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                    if (targetCamera == null)
                    {
                        targetCamera = FindFirstObjectByType<Camera>();
                    }
                }

                if (targetCamera == null)
                {
                    Debug.LogError("ScavengerGameManager: No camera found! Cannot create feedback text.");
                    return;
                }

                // Create Canvas for feedback text
                GameObject canvasObj = new GameObject("FeedbackCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                
                // Use Screen Space - Camera mode for VR compatibility
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = targetCamera;
                canvas.planeDistance = 0.5f; // Same distance as other UI
                canvas.sortingOrder = 10002; // Above everything

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();

                // Create Text object
                feedbackTextObject = new GameObject("FeedbackText");
                feedbackTextObject.transform.SetParent(canvasObj.transform, false);

                RectTransform rectTransform = feedbackTextObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
                rectTransform.sizeDelta = new Vector2(1000, 150);
                rectTransform.anchoredPosition = Vector2.zero;

                feedbackText = feedbackTextObject.AddComponent<Text>();
                feedbackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                feedbackText.fontSize = 24; // Same as other text
                feedbackText.fontStyle = FontStyle.Bold;
                feedbackText.alignment = TextAnchor.MiddleCenter;
                feedbackText.color = Color.red;
            }
            else
            {
                feedbackTextObject = existingText;
                feedbackText = existingText.GetComponent<Text>();
            }
        }

        // Hide feedback text initially
        if (feedbackTextObject != null)
        {
            feedbackTextObject.SetActive(false);
        }
    }

    public void ShowFeedback(string message, float duration = 2.0f)
    {
        if (feedbackTextObject != null && feedbackText != null)
        {
            feedbackText.text = message;
            feedbackTextObject.SetActive(true);
            
            // Hide after duration
            StartCoroutine(HideFeedbackAfterDelay(duration));
        }
    }

    System.Collections.IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (feedbackTextObject != null)
        {
            feedbackTextObject.SetActive(false);
        }
    }

    public void OnWorldChanged(bool isInVR)
    {
        // Update item visibility when world changes
        UpdateItemVisibility();
    }

    void UpdateItemVisibility()
    {
        // Early return if not initialized yet or game hasn't started
        if (itemWorldTypes == null || items == null || items.Count == 0 || !gameStarted)
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
        Debug.Log($"ScavengerGameManager: ObjectFound called with item: {(item != null ? item.name : "null")}");

        // Early return if not initialized or game hasn't started
        if (itemWorldTypes == null || items == null || items.Count == 0 || !gameStarted)
        {
            Debug.LogWarning("ScavengerGameManager: Not initialized yet or game hasn't started!");
            return;
        }

        if (item == null)
        {
            Debug.LogWarning("ScavengerGameManager: Item is null!");
            return;
        }

        // Find the item in our list (check by reference and by name)
        int foundItemIndex = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item || (items[i] != null && items[i].name == item.name))
            {
                foundItemIndex = i;
                break;
            }
        }

        Debug.Log($"ScavengerGameManager: Found item index: {foundItemIndex}, currentItemIndex: {currentItemIndex}");

        // Check if the found item is the current item we're looking for
        if (foundItemIndex == -1)
        {
            Debug.LogWarning($"ScavengerGameManager: Item {item.name} not found in items list!");
            return;
        }

        if (foundItemIndex != currentItemIndex)
        {
            Debug.Log($"ScavengerGameManager: Item {item.name} is not the current target (current: {currentItemIndex}, found: {foundItemIndex})");
            return;
        }

        // Only allow interaction if we're in the correct world for the current item
        bool currentItemIsVR = itemWorldTypes[currentItemIndex];
        bool isInVR = RealityManager.Instance != null && RealityManager.Instance.IsInVR;

        Debug.Log($"ScavengerGameManager: Item is {(currentItemIsVR ? "VR" : "AR")}, current world is {(isInVR ? "VR" : "AR")}");

        if (currentItemIsVR != isInVR)
        {
            Debug.LogWarning($"ScavengerGameManager: Cannot collect - wrong world!");
            return;
        }

        Debug.Log($"ScavengerGameManager: Collecting item {currentItemIndex}!");

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
            // All items found - but don't show victory yet, wait for chest
            allItemsCollected = true;
            Debug.Log("ScavengerGameManager: All items collected! Now open the chest!");
        }
        else
        {
            // Update visibility for the next item (will show when player switches to correct world)
            UpdateItemVisibility();
        }
    }

    public void OnChestOpened()
    {
        if (!allItemsCollected)
        {
            Debug.LogWarning("ScavengerGameManager: Chest opened but not all items collected!");
            return;
        }

        gameCompleted = true;
        ShowVictory();
    }

    void ShowVictory()
    {
        Debug.Log("Victory! All items collected and chest opened!");

        // Update victory text with current message
        if (victoryText != null)
        {
            victoryText.text = victoryMessage;
        }

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

    void Update()
    {
        // Check for restart trigger after game completion (LEFT trigger, not right)
        if (gameCompleted && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            RestartGame();
        }
    }

    void RestartGame()
    {
        Debug.Log("ScavengerGameManager: Restarting game...");

        // Hide victory text
        if (victoryTextObject != null)
        {
            victoryTextObject.SetActive(false);
        }

        // Hide feedback text if visible
        if (feedbackTextObject != null)
        {
            feedbackTextObject.SetActive(false);
        }

        // Reset game state FIRST
        gameStarted = false;
        gameCompleted = false;
        allItemsCollected = false;
        currentItemIndex = 0;

        // Hide all items
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetActive(false);
            }
        }

        // Switch back to AR mode
        if (RealityManager.Instance != null)
        {
            RealityManager.Instance.SwitchToAR();
            Debug.Log("ScavengerGameManager: Switched back to AR mode on restart");
        }

        // Wait a frame to ensure everything is reset, then show welcome screen
        StartCoroutine(ShowWelcomeAfterRestart());
    }

    System.Collections.IEnumerator ShowWelcomeAfterRestart()
    {
        // Wait a frame to ensure all state is reset
        yield return null;
        yield return null;

        // Show welcome screen again
        if (GameStartUI.Instance != null)
        {
            Debug.Log("ScavengerGameManager: Showing welcome screen on restart");
            GameStartUI.Instance.ShowWelcomeScreen();
        }
        else
        {
            Debug.LogWarning("ScavengerGameManager: GameStartUI.Instance is null! Cannot show welcome screen.");
        }
    }
}

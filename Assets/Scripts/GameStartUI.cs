using UnityEngine;
using UnityEngine.UI;

public class GameStartUI : MonoBehaviour
{
    public static GameStartUI Instance { get; private set; }

    [Header("UI References")]
    public Canvas welcomeCanvas;
    public Text welcomeTitleText;
    public Text instructionsText;

    [Header("Settings")]
    [TextArea(3, 10)]
    public string welcomeTitle = "Welcome to The Hybrid Hunt!";
    [TextArea(5, 15)]
    public string instructions = "Find all the collectibles!\n\n" +
                                 "Press RIGHT TRIGGER to start the game.\n\n" +
                                 "Use your right controller to point and shoot collectibles.\n" +
                                 "Press A/B/X to switch between AR and VR worlds.\n" +
                                 "Collect all items, then open the chest to win!";

    private bool gameStarted = false;

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
        Debug.Log("GameStartUI: Start() called");

        // Create UI if not assigned
        if (welcomeCanvas == null)
        {
            Debug.Log("GameStartUI: Creating welcome UI...");
            CreateWelcomeUI();
        }
        else
        {
            Debug.Log("GameStartUI: Using existing welcome canvas");
        }

        // Show welcome screen initially
        ShowWelcomeScreen();
    }

    void Update()
    {
        // Check for right trigger to start game
        if (!gameStarted && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            OnStartTriggerPressed();
        }
    }

    void CreateWelcomeUI()
    {
        // Get the camera (for VR compatibility)
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            Debug.LogError("GameStartUI: No camera found! Cannot create UI.");
            return;
        }

        Debug.Log($"GameStartUI: Using camera: {targetCamera.name}");

        // Create Canvas
        GameObject canvasObj = new GameObject("WelcomeCanvas");
        welcomeCanvas = canvasObj.AddComponent<Canvas>();
        
        // Use Screen Space - Camera mode for VR compatibility
        welcomeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        welcomeCanvas.worldCamera = targetCamera;
        welcomeCanvas.planeDistance = 0.5f; // Close to camera
        welcomeCanvas.sortingOrder = 10001; // Above victory text

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("GameStartUI: Canvas created successfully");

        // Create background panel
        GameObject panelObj = new GameObject("BackgroundPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        // Create title text
        GameObject titleObj = new GameObject("WelcomeTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        welcomeTitleText = titleObj.AddComponent<Text>();
        welcomeTitleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        welcomeTitleText.fontSize = 24; // Small font size
        welcomeTitleText.fontStyle = FontStyle.Bold;
        welcomeTitleText.alignment = TextAnchor.MiddleCenter;
        welcomeTitleText.color = Color.white;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.65f);
        titleRect.anchorMax = new Vector2(0.5f, 0.65f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = Vector2.zero;

        // Create instructions text
        GameObject instructionsObj = new GameObject("InstructionsText");
        instructionsObj.transform.SetParent(canvasObj.transform, false);
        instructionsText = instructionsObj.AddComponent<Text>();
        instructionsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        instructionsText.fontSize = 18; // Small font size
        instructionsText.alignment = TextAnchor.MiddleCenter;
        instructionsText.color = Color.white;

        RectTransform instructionsRect = instructionsObj.GetComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.5f, 0.4f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0.4f);
        instructionsRect.sizeDelta = new Vector2(800, 250);
        instructionsRect.anchoredPosition = Vector2.zero;
    }

    public void ShowWelcomeScreen()
    {
        gameStarted = false; // Reset game started flag
        Debug.Log("GameStartUI: Showing welcome screen");

        // Ensure canvas exists
        if (welcomeCanvas == null)
        {
            Debug.LogWarning("GameStartUI: Welcome canvas is null, recreating...");
            CreateWelcomeUI();
        }

        if (welcomeCanvas != null)
        {
            welcomeCanvas.gameObject.SetActive(true);
            Debug.Log("GameStartUI: Canvas activated");
        }
        else
        {
            Debug.LogError("GameStartUI: Welcome canvas is still null after recreation!");
            return;
        }

        // Update text content
        if (welcomeTitleText != null)
        {
            welcomeTitleText.text = welcomeTitle;
            Debug.Log($"GameStartUI: Title text set to: {welcomeTitle}");
        }
        else
        {
            Debug.LogWarning("GameStartUI: Welcome title text is null!");
        }

        if (instructionsText != null)
        {
            instructionsText.text = instructions;
            Debug.Log("GameStartUI: Instructions text set");
        }
        else
        {
            Debug.LogWarning("GameStartUI: Instructions text is null!");
        }
    }

    void HideWelcomeScreen()
    {
        if (welcomeCanvas != null)
        {
            welcomeCanvas.gameObject.SetActive(false);
        }
    }

    void OnStartTriggerPressed()
    {
        if (gameStarted)
        {
            return;
        }

        gameStarted = true;
        Debug.Log("Game Start triggered by right trigger!");

        // Hide welcome screen
        HideWelcomeScreen();

        // Notify ScavengerGameManager to start the game
        if (ScavengerGameManager.Instance != null)
        {
            ScavengerGameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("GameStartUI: ScavengerGameManager.Instance is null!");
        }
    }
}


using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Game Stats")]
    public int totalScore = 0;
    public List<string> collectedItems = new List<string>();

    [Header("UI References")]
    public Text scoreText;
    public Text itemsText;

    [Header("Multiplayer Setup")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("GameManager Started - Scene: " + SceneManager.GetActiveScene().name);

        // Setup UI
        UpdateUI();

        // Handle multiplayer spawning if we're in a room
        if (PhotonNetwork.InRoom)
        {
            SetupMultiplayer();
        }
    }

    private void SetupMultiplayer()
    {
        Debug.Log("Setting up multiplayer...");
        Debug.Log($"In room: {PhotonNetwork.CurrentRoom.Name} with {PhotonNetwork.CurrentRoom.PlayerCount} players");

        // Setup camera
        SetupCamera();

        // Spawn player
        SpawnPlayer();
    }

    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            Debug.Log("Main camera activated: " + mainCamera.name);
        }
        else
        {
            Debug.LogError("Main camera not found in scene!");
        }
    }

    private void SpawnPlayer()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("Cannot spawn - not connected to Photon or not in room!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned in GameManager!");
            return;
        }

        try
        {
            Vector3 spawnPosition = GetSpawnPosition();
            Debug.Log($"Spawning player at position: {spawnPosition}");

            GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);

            if (player != null)
            {
                Debug.Log($"Player spawned successfully: {player.name}");

                // Setup camera to follow this player if it's ours
                if (player.GetComponent<PhotonView>().IsMine)
                {
                    SetupPlayerCamera(player);
                }
            }
            else
            {
                Debug.LogError("Failed to spawn player prefab!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Spawning failed: {e.Message}");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned, using default position");
            return Vector3.zero;
        }

        int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        int spawnIndex = playerNumber % spawnPoints.Length;

        Debug.Log($"Player {PhotonNetwork.LocalPlayer.ActorNumber} using spawn point {spawnIndex}");
        return spawnPoints[spawnIndex].position;
    }

    private void SetupPlayerCamera(GameObject player)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Add camera follow component if it doesn't exist
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            cameraFollow.target = player.transform;
            Debug.Log("Camera following player: " + player.name);
        }
    }

    // Your original methods - MAKE SURE THESE EXIST AND ARE PUBLIC
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateUI();

        // Optional: Sync score across network if needed
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Score updated: {totalScore}");
        }
    }

    public void AddCollectedItem(string itemName)
    {
        collectedItems.Add(itemName);
        UpdateUI();

        // Optional: Sync collected items across network if needed
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Item collected: {itemName}");
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }

        if (itemsText != null)
        {
            itemsText.text = $"Items: {collectedItems.Count}";
        }
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        Debug.Log("GameManager: Joined room successfully");

        // If we're already in the game scene, setup multiplayer
        if (SceneManager.GetActiveScene().name == "GameScene") // Replace with your game scene name
        {
            SetupMultiplayer();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room, returning to lobby...");

        // Clear game data when leaving room
        totalScore = 0;
        collectedItems.Clear();
        UpdateUI();

        // Load lobby scene
        SceneManager.LoadScene("Lobby"); // Replace with your lobby scene name
    }

    // Handle scene changes
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Re-find UI references when new scene loads
        FindUIReferences();

        // Update UI with current data
        UpdateUI();

        // Setup multiplayer if we're in a game scene and in a room
        if (PhotonNetwork.InRoom && scene.name == "GameScene") // Replace with your game scene name
        {
            // Small delay to ensure scene is fully loaded
            Invoke(nameof(SetupMultiplayer), 0.1f);
        }
    }

    private void FindUIReferences()
    {
        // Find UI elements in the current scene
        GameObject scoreObj = GameObject.Find("ScoreText"); // Replace with your UI element names
        GameObject itemsObj = GameObject.Find("ItemsText");

        if (scoreObj != null) scoreText = scoreObj.GetComponent<Text>();
        if (itemsObj != null) itemsText = itemsObj.GetComponent<Text>();

        Debug.Log($"UI References - Score: {scoreText != null}, Items: {itemsText != null}");
    }

    // Debug method for testing
    private void Update()
    {
        // Press R to respawn (for testing)
        if (Input.GetKeyDown(KeyCode.R) && PhotonNetwork.InRoom)
        {
            Debug.Log("Manual respawn triggered");
            SpawnPlayer();
        }

        // Press T to test score (for testing)
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddScore(10);
            AddCollectedItem("TestItem");
        }
    }
}
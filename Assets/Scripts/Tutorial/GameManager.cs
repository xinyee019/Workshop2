using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Stats")]
    public int totalScore = 0;
    public List<string> collectedItems = new List<string>();

    [Header("UI References")]
    public Text scoreText;
    public Text itemsText;

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

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateUI();
    }

    public void AddCollectedItem(string itemName)
    {
        collectedItems.Add(itemName);
        UpdateUI();
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
}
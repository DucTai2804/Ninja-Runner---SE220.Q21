using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Thư viện để làm việc với TextMeshPro
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public GameState currentState = GameState.Menu;
    public float score = 0f;
    public int coins = 0;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject inGamePanel;
    public GameObject gameOverPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI finalScoreText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Khi game mới bật, hiển thị Menu và dừng thời gian
        SetState(GameState.Menu);

        // Khởi tạo UIManager tự động sinh UI nếu chưa có
        if (UIManager.Instance == null)
        {
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManagerObj.AddComponent<UIManager>();
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            // Tăng điểm tự động dựa trên tốc độ hiện tại của thế giới
            if (WorldManager.Instance != null)
            {
                score += WorldManager.Instance.currentSpeed * Time.deltaTime * 0.5f;
            }
            else
            {
                score += 10f * Time.deltaTime; // Fallback nếu quên gắn WorldManager
            }
            
            UpdateUI();
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        // Bật/tắt các Panel tương ứng
        if (mainMenuPanel) mainMenuPanel.SetActive(currentState == GameState.Menu);
        if (inGamePanel) inGamePanel.SetActive(currentState == GameState.Playing);
        if (gameOverPanel) gameOverPanel.SetActive(currentState == GameState.GameOver);

        switch (currentState)
        {
            case GameState.Menu:
                Time.timeScale = 0f; // Dừng mọi hoạt động
                break;
            case GameState.Playing:
                Time.timeScale = 1f; // Tiếp tục game
                score = 0f;
                coins = 0;
                UpdateUI();
                break;
            case GameState.GameOver:
                Time.timeScale = 0f; // Dừng mọi hoạt động
                if (finalScoreText) finalScoreText.text = "FINAL SCORE\n" + Mathf.FloorToInt(score).ToString();
                if (UIManager.Instance != null) UIManager.Instance.ShowGameOver(); // Hiện chữ Game Over
                break;
        }
    }

    public void AddCoin()
    {
        if (currentState != GameState.Playing) return;
        coins++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText) scoreText.text = "SCORE: " + Mathf.FloorToInt(score).ToString();
        if (coinsText) coinsText.text = "COINS: " + coins.ToString();
    }

    // Gắn vào Nút Bắt Đầu (Play Button)
    public void StartGame()
    {
        SetState(GameState.Playing);
    }

    // Gọi khi Sasuke đụng chướng ngại vật
    public void GameOver()
    {
        if (currentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    // Gắn vào Nút Chơi Lại (Retry Button)
    public void RestartGame()
    {
        Time.timeScale = 1f; // Trả lại thời gian trước khi reload scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Gắn vào Nút Thoát (Quit Button)
    public void QuitGame()
    {
        Debug.Log("Game Exiting...");
        Application.Quit();
    }
}

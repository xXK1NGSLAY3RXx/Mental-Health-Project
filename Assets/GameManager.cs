// GameManager.cs
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Maximum score for this level")]
    public int maxScore = 100;

    [Tooltip("UI Image (Type=Filled, Fill Method=Vertical)")]
    public Image scoreBar;

    private int currentScore = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentScore = 0;
        UpdateScoreBar();
    }

    public void AddScore(int points)
    {
        currentScore = Mathf.Clamp(currentScore + points, 0, maxScore);
        UpdateScoreBar();
    }

    private void UpdateScoreBar()
    {
        if (scoreBar != null && maxScore > 0)
            scoreBar.fillAmount = (float)currentScore / maxScore;
    }

    public void EndLevel()
    {
        Debug.Log($"Level complete! Final score: {currentScore}");
        // TODO: hook in your post-level UI here.
    }
}

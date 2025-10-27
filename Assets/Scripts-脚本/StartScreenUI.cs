using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Start Screen UI controller for PacStudent game
/// Handles title screen appearance and functionality
/// </summary>
public class StartScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image pacStudentSprite;
    [SerializeField] private Image ghostSprite;
    [SerializeField] private Button btnLevel1;
    [SerializeField] private Button btnLevel2;
    [SerializeField] private TextMeshProUGUI level1HighScoreText;
    [SerializeField] private TextMeshProUGUI level1BestTimeText;
    [SerializeField] private TextMeshProUGUI level2HighScoreText;
    [SerializeField] private TextMeshProUGUI level2BestTimeText;
    
    [Header("Border Animation")]
    [SerializeField] private RectTransform[] borderElements;
    [SerializeField] private float borderAnimationSpeed = 2f;
    
    [Header("Sprites")]
    [SerializeField] private Sprite pacStudentSpriteAsset;
    [SerializeField] private Sprite ghostSpriteAsset;
    
    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    private AudioSource audioSource;
    
    [Header("Scenes")]
    [SerializeField] private string level1SceneName = "A4 new";
    [SerializeField] private string level2SceneName = "RecreatedLevel";
    
    private Vector3[] borderOriginalPositions;
    private float timeElapsed = 0f;
    
    void Start()
    {
        InitializeUI();
        SetupButtons();
        LoadStats();
        PlayBackgroundMusic();
        
        // Store original positions for border animation
        if (borderElements != null && borderElements.Length > 0)
        {
            borderOriginalPositions = new Vector3[borderElements.Length];
            for (int i = 0; i < borderElements.Length; i++)
            {
                if (borderElements[i] != null)
                    borderOriginalPositions[i] = borderElements[i].anchoredPosition;
            }
        }
    }
    
    void Update()
    {
        AnimateBorder();
    }
    
    void InitializeUI()
    {
        // Set title - 改为 Campus Maze
        if (titleText != null)
        {
            titleText.text = "Campus Maze";
            titleText.color = Color.yellow;
        }
        
        // Set subtitle
        if (subtitleText != null)
        {
            subtitleText.text = "Escape the Maze!";
            subtitleText.color = Color.white;
        }
        
        // Set sprites
        if (pacStudentSprite != null && pacStudentSpriteAsset != null)
            pacStudentSprite.sprite = pacStudentSpriteAsset;
            
        if (ghostSprite != null && ghostSpriteAsset != null)
            ghostSprite.sprite = ghostSpriteAsset;
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void SetupButtons()
    {
        // Level 1 button
        if (btnLevel1 != null)
        {
            btnLevel1.onClick.AddListener(OnLevel1Clicked);
        }
        
        // Level 2 button
        if (btnLevel2 != null)
        {
            btnLevel2.onClick.AddListener(OnLevel2Clicked);
        }
    }
    
    void LoadStats()
    {
        // Load high scores and best times from PlayerPrefs or set defaults
        int level1HighScore = PlayerPrefs.GetInt("Level1HighScore", 0);
        int level2HighScore = PlayerPrefs.GetInt("Level2HighScore", 0);
        string level1BestTime = PlayerPrefs.GetString("Level1BestTime", "00:00:00");
        string level2BestTime = PlayerPrefs.GetString("Level2BestTime", "00:00:00");
        
        if (level1HighScoreText != null)
            level1HighScoreText.text = $"High Score: {level1HighScore:000000}";
            
        if (level1BestTimeText != null)
            level1BestTimeText.text = $"Best Time: {level1BestTime}";
            
        if (level2HighScoreText != null)
            level2HighScoreText.text = $"High Score: {level2HighScore:000000}";
            
        if (level2BestTimeText != null)
            level2BestTimeText.text = $"Best Time: {level2BestTime}";
    }
    
    void PlayBackgroundMusic()
    {
        if (audioSource != null && backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    void AnimateBorder()
    {
        if (borderElements == null || borderElements.Length == 0)
            return;
            
        timeElapsed += Time.deltaTime * borderAnimationSpeed;
        
        // Animate border elements with a wave effect
        for (int i = 0; i < borderElements.Length; i++)
        {
            if (borderElements[i] == null)
                continue;
                
            float offset = Mathf.Sin(timeElapsed + i) * 10f;
            Vector3 newPos = borderOriginalPositions[i];
            
            // Apply animation based on element type
            if (i % 2 == 0)
            {
                newPos.x += offset;
            }
            else
            {
                newPos.y += offset;
            }
            
            borderElements[i].anchoredPosition = newPos;
        }
    }
    
    void OnLevel1Clicked()
    {
        PlayButtonClickSound();
        SceneManager.LoadScene(level1SceneName);
    }
    
    void OnLevel2Clicked()
    {
        PlayButtonClickSound();
        SceneManager.LoadScene(level2SceneName);
    }
    
    void OnButtonHover(Button button)
    {
        PlayButtonHoverSound();
        
        // Visual feedback - scale up slightly
        if (button != null)
        {
            button.transform.localScale = Vector3.one * 1.1f;
            
            // Create a coroutine to scale back down
            StartCoroutine(ScaleDownButton(button));
        }
    }
    
    System.Collections.IEnumerator ScaleDownButton(Button button)
    {
        yield return new WaitForSeconds(0.2f);
        
        if (button != null)
        {
            button.transform.localScale = Vector3.one;
        }
    }
    
    void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
    
    void PlayButtonHoverSound()
    {
        if (audioSource != null && buttonHoverSound != null)
        {
            audioSource.PlayOneShot(buttonHoverSound);
        }
    }
}

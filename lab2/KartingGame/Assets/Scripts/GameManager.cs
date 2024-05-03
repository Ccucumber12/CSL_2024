using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogError("Game manager is NULL");
            return _instance;
        }
    }

    public int maxCheckpoint;
    public PlayerControl playerControl;
    public TextMeshProUGUI timerText;
    public AudioSource countdownSFX;
    public AudioSource bgm;
    
    private int checkpointCount;
    private bool isRacing;
    private float startTime;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(StartCountdown());
    }

    private void Update()
    {
        if (isRacing)
        {
            UpdateTimer();
        }
    }
    private IEnumerator StartCountdown()
    {
        yield return new WaitForSeconds(1);
        countdownSFX.Play();
        yield return new WaitForSeconds(3);
        playerControl.EnableMovement();
        startTime = Time.time;
        isRacing = true;
        bgm.Play();
    }

    private void UpdateTimer()
    {
        float currentTime = Time.time - startTime;
        currentTime = Mathf.Max(0, currentTime);
        int minutes = (int)(currentTime / 60);
        float seconds = (currentTime - minutes * 60);
        string displayTime = string.Format("{0}:{1:00.000}", minutes, seconds);
        timerText.text = displayTime;
    }

    public void CheckpointPassed()
    {
        checkpointCount++;
    }

    public bool GoalCheck()
    {
        return checkpointCount == maxCheckpoint;
    }

    public void GoalPassed()
    {
        isRacing = false;
        playerControl.DisableMovement();
        bgm.volume = 0.2f;
    }
}

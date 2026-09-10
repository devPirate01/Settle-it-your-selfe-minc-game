using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameIntroManager : MonoBehaviour
{
    public static GameIntroManager Instance;

    [Header("Cameras")]
    [Tooltip("The camera positioned in the boss's office")]
    public Camera introCamera;
    [Tooltip("The split-screen cameras for Player 1 and Player 2")]
    public Camera[] playerCameras;

    [Header("Promotion Round Timer & Sudden Death")]
    [Tooltip("Round duration in seconds (300 = 5 minutes). Easily tunable in Inspector!")]
    public float roundDuration = 300f;

    [Header("Optional Legacy UI Slots (UI Toolkit handles this automatically)")]
    public GameObject menuPanel;
    public TextMeshProUGUI bossDialogueText;
    public TextMeshProUGUI fightBannerText;
    public GameObject rematchButton;

    [Header("Camera Shake Tuning")]
    public float shoutShakeIntensity = 0.35f;
    public float shoutShakeDuration = 0.6f;

    bool gameStarted = false;
    float currentTimer;
    bool timerRunning = false;
    bool isSuddenDeath = false;

    SimpleMovement[] playersMovement;
    Grabber[] playersGrabber;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playersMovement = FindObjectsByType<SimpleMovement>(FindObjectsSortMode.None);
        playersGrabber = FindObjectsByType<Grabber>(FindObjectsSortMode.None);

        // Freeze players at start
        SetPlayersControl(false);

        // Activate intro camera, deactivate split-screen cameras
        if (introCamera) introCamera.enabled = true;
        if (playerCameras != null)
        {
            foreach (var cam in playerCameras) if (cam) cam.enabled = false;
        }

        // Initialize timer display
        currentTimer = roundDuration;
        GameUIController.Instance?.UpdateTimer(currentTimer, false);

        // Show UI Toolkit menu or legacy menu
        GameUIController.Instance?.ShowMenu(true);
        if (menuPanel) menuPanel.SetActive(true);
    }

    void Update()
    {
        if (!gameStarted)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            {
                StartGameSequence();
            }
        }
        else
        {
            // Timer countdown
            if (timerRunning && !isSuddenDeath)
            {
                currentTimer -= Time.deltaTime;
                GameUIController.Instance?.UpdateTimer(currentTimer, false);

                if (currentTimer <= 0f)
                {
                    TriggerSuddenDeath();
                }
            }

            // Quick restart with R key
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
        }
    }

    public void StartGameSequence()
    {
        if (gameStarted) return;
        gameStarted = true;
        StartCoroutine(BossIntroRoutine());
    }

    IEnumerator BossIntroRoutine()
    {
        // 1. Hide menu
        GameUIController.Instance?.ShowMenu(false);
        if (menuPanel) menuPanel.SetActive(false);

        // 2. Boss Line 1
        string line1 = "You two want that promotion?!";
        GameUIController.Instance?.ShowBossDialogue(line1, false);
        if (bossDialogueText)
        {
            bossDialogueText.gameObject.SetActive(true);
            bossDialogueText.text = $"<b>BOSS:</b> \"{line1}\"";
        }
        yield return new WaitForSeconds(1.8f);

        // 3. Boss Line 2 (The shout!)
        string shout = "SETTLE IT YOURSELVES!!!";
        GameUIController.Instance?.ShowBossDialogue(shout, true);
        if (bossDialogueText)
        {
            bossDialogueText.text = $"<size=125%><color=#FF2222><b>BOSS:</b> \"{shout}\"</color></size>";
        }

        // Camera shake
        if (introCamera) StartCoroutine(ShakeCamera(introCamera.transform, shoutShakeDuration, shoutShakeIntensity));
        yield return new WaitForSeconds(1.6f);

        // 4. Switch to split-screen cameras
        GameUIController.Instance?.HideBossDialogue();
        if (bossDialogueText) bossDialogueText.gameObject.SetActive(false);
        if (introCamera) introCamera.enabled = false;

        if (playerCameras != null)
        {
            foreach (var cam in playerCameras) if (cam) cam.enabled = true;
        }

        // 5. Flash "FIGHT!"
        GameUIController.Instance?.ShowFightBanner(true);
        if (fightBannerText)
        {
            fightBannerText.gameObject.SetActive(true);
            fightBannerText.text = "<color=#FFE600><b>FIGHT!</b></color>";
        }

        // 6. Enable player controls and start the round timer!
        SetPlayersControl(true);
        currentTimer = roundDuration;
        timerRunning = true;
        isSuddenDeath = false;

        yield return new WaitForSeconds(1.2f);
        GameUIController.Instance?.ShowFightBanner(false);
        if (fightBannerText) fightBannerText.gameObject.SetActive(false);
    }

    public void TriggerSuddenDeath()
    {
        isSuddenDeath = true;
        GameUIController.Instance?.UpdateTimer(0, true);

        // Notify players with dramatic alert
        GameUIController.Instance?.ShowBossDialogue("SUDDEN DEATH! 1 HP EACH! FIRST HIT WINS!", true);
        StartCoroutine(HideSuddenDeathAlert());

        // Drop all alive players to 1 HP
        var allHealths = FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (var h in allHealths)
        {
            h.SetSuddenDeath();
        }
    }

    IEnumerator HideSuddenDeathAlert()
    {
        yield return new WaitForSeconds(3.5f);
        GameUIController.Instance?.HideBossDialogue();
    }

    void SetPlayersControl(bool enabled)
    {
        if (playersMovement != null)
        {
            foreach (var p in playersMovement) if (p) p.enabled = enabled;
        }
        if (playersGrabber != null)
        {
            foreach (var g in playersGrabber) if (g) g.enabled = enabled;
        }
    }

    IEnumerator ShakeCamera(Transform camTransform, float duration, float intensity)
    {
        Vector3 originalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            camTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
    }

    public void OnGameOver()
    {
        timerRunning = false;
        if (rematchButton) rematchButton.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

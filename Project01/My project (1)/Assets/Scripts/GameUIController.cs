using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance;

    UIDocument uiDoc;
    VisualElement root;

    // Layers
    VisualElement hudLayer;
    VisualElement menuLayer;
    VisualElement bossLayer;
    VisualElement bannerLayer;
    VisualElement victoryLayer;
    VisualElement drawLayer;

    // Elements
    VisualElement p1Fill;
    VisualElement p2Fill;
    Label timerLabel;
    Label bossText;
    Label fightText;
    Label victoryTitle;
    Button startBtn;
    Button rematchBtn;
    Button drawRestartBtn;

    // Input Mode Settings Elements
    Button modeKeyboardBtn;
    Button modeControllerBtn;
    Label controllerStatus;
    Label p1ControlLine;
    Label p2ControlLine;
    Label startHintText;

    // Slack Notification Elements
    VisualElement slackToast;
    Label slackSender;
    Label slackMessage;
    Coroutine slackToastRoutine;

    float statusCheckTimer;

    void Awake()
    {
        Instance = this;
        uiDoc = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        if (uiDoc == null) uiDoc = GetComponent<UIDocument>();
        root = uiDoc.rootVisualElement;
        if (root == null) return;

        // Query layers
        hudLayer = root.Q<VisualElement>("hud-layer");
        menuLayer = root.Q<VisualElement>("menu-layer");
        bossLayer = root.Q<VisualElement>("boss-layer");
        bannerLayer = root.Q<VisualElement>("banner-layer");
        victoryLayer = root.Q<VisualElement>("victory-layer");
        drawLayer = root.Q<VisualElement>("draw-layer");

        // Query controls
        p1Fill = root.Q<VisualElement>("p1-health-fill");
        p2Fill = root.Q<VisualElement>("p2-health-fill");
        timerLabel = root.Q<Label>("timer-label");
        bossText = root.Q<Label>("boss-text");
        fightText = root.Q<Label>("fight-text");
        victoryTitle = root.Q<Label>("victory-title");
        startBtn = root.Q<Button>("start-btn");
        rematchBtn = root.Q<Button>("rematch-btn");
        drawRestartBtn = root.Q<Button>("draw-restart-btn");

        // Query Input Mode elements
        modeKeyboardBtn = root.Q<Button>("btn-mode-keyboard");
        modeControllerBtn = root.Q<Button>("btn-mode-controller");
        controllerStatus = root.Q<Label>("controller-status");
        p1ControlLine = root.Q<Label>("p1-control-line");
        p2ControlLine = root.Q<Label>("p2-control-line");
        startHintText = root.Q<Label>("start-hint-text");

        // Query Slack elements
        slackToast = root.Q<VisualElement>("slack-toast");
        slackSender = root.Q<Label>("slack-sender");
        slackMessage = root.Q<Label>("slack-message");

        // Bind clicks
        if (startBtn != null) startBtn.clicked += OnStartClicked;
        if (rematchBtn != null) rematchBtn.clicked += OnRematchClicked;
        if (drawRestartBtn != null) drawRestartBtn.clicked += OnRematchClicked;

        if (modeKeyboardBtn != null) modeKeyboardBtn.clicked += () => SetInputMode(InputMode.Keyboard);
        if (modeControllerBtn != null) modeControllerBtn.clicked += () => SetInputMode(InputMode.DualController);

        // Initial setup
        SetInputMode(MatchInputManager.CurrentMode);
        UpdateControllerStatus();

        // Default initial state
        ShowMenu(true);
        HideBossDialogue();
        ShowFightBanner(false);
        if (victoryLayer != null) victoryLayer.style.display = DisplayStyle.None;
        if (drawLayer != null) drawLayer.style.display = DisplayStyle.None;
        if (slackToast != null)
        {
            slackToast.RemoveFromClassList("slack-toast-visible");
            slackToast.AddToClassList("slack-toast-hidden");
        }
    }

    void OnDisable()
    {
        if (startBtn != null) startBtn.clicked -= OnStartClicked;
        if (rematchBtn != null) rematchBtn.clicked -= OnRematchClicked;
        if (drawRestartBtn != null) drawRestartBtn.clicked -= OnRematchClicked;
    }

    void Update()
    {
        // Periodically refresh controller connection status while menu is visible
        if (menuLayer != null && menuLayer.style.display == DisplayStyle.Flex)
        {
            statusCheckTimer -= Time.deltaTime;
            if (statusCheckTimer <= 0f)
            {
                statusCheckTimer = 0.5f;
                UpdateControllerStatus();
            }
        }
    }

    public void SetInputMode(InputMode mode)
    {
        MatchInputManager.SetMode(mode);

        if (mode == InputMode.Keyboard)
        {
            if (modeKeyboardBtn != null) modeKeyboardBtn.AddToClassList("mode-btn-active");
            if (modeControllerBtn != null) modeControllerBtn.RemoveFromClassList("mode-btn-active");

            if (p1ControlLine != null) p1ControlLine.text = "P1: Move (W/S)  Turn (A/D)  Grab (E)  Throw/Punch (Q)";
            if (p2ControlLine != null) p2ControlLine.text = "P2: Move/Turn (Arrows)  Grab (R-Shift)  Throw/Punch (Num0)";
            if (startHintText != null) startHintText.text = "Press SPACE or click to start";
        }
        else
        {
            if (modeControllerBtn != null) modeControllerBtn.AddToClassList("mode-btn-active");
            if (modeKeyboardBtn != null) modeKeyboardBtn.RemoveFromClassList("mode-btn-active");

            if (p1ControlLine != null) p1ControlLine.text = "P1: Move (L-Stick)  Look (R-Stick)  Grab (LT/A/B)  Throw/Punch (RT/X/Y)";
            if (p2ControlLine != null) p2ControlLine.text = "P2: Move (L-Stick)  Look (R-Stick)  Grab (LT/A/B)  Throw/Punch (RT/X/Y)";
            if (startHintText != null) startHintText.text = "Press START or A on controller to start";
        }

        UpdateControllerStatus();
    }

    void UpdateControllerStatus()
    {
        if (controllerStatus == null) return;

        int count = MatchInputManager.ConnectedGamepadCount;
        if (count >= 2)
        {
            controllerStatus.text = $"🎮 {count} Xbox Controllers Connected (Ready!)";
            controllerStatus.RemoveFromClassList("status-warn");
            controllerStatus.AddToClassList("status-ok");
        }
        else if (count == 1)
        {
            controllerStatus.text = "⚠️ 1 Controller Connected (Plug in 2nd for P2)";
            controllerStatus.RemoveFromClassList("status-ok");
            controllerStatus.AddToClassList("status-warn");
        }
        else
        {
            controllerStatus.text = "⚠️ 0 Controllers Detected (Check USB/Bluetooth)";
            controllerStatus.RemoveFromClassList("status-ok");
            controllerStatus.AddToClassList("status-warn");
        }
    }

    void OnStartClicked()
    {
        GameIntroManager.Instance?.StartGameSequence();
    }

    void OnRematchClicked()
    {
        GameIntroManager.Instance?.RestartGame();
    }

    public void ShowMenu(bool show)
    {
        if (menuLayer != null)
        {
            menuLayer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void ShowBossDialogue(string message, bool isShout = false)
    {
        if (bossLayer != null)
        {
            bossLayer.style.display = DisplayStyle.Flex;
            if (bossText != null)
            {
                bossText.text = message;
                bossText.style.color = isShout ? new StyleColor(new Color(1f, 0.2f, 0.2f)) : new StyleColor(Color.white);
                bossText.style.fontSize = isShout ? 32 : 24;
            }
        }
    }

    public void HideBossDialogue()
    {
        if (bossLayer != null)
        {
            bossLayer.style.display = DisplayStyle.None;
        }
    }

    public void ShowFightBanner(bool show)
    {
        if (bannerLayer != null)
        {
            bannerLayer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void SetHealth(int playerIndex, float normalized)
    {
        float percent = Mathf.Clamp01(normalized) * 100f;
        if (playerIndex == 0 && p1Fill != null)
        {
            p1Fill.style.width = Length.Percent(percent);
        }
        else if (playerIndex == 1 && p2Fill != null)
        {
            p2Fill.style.width = Length.Percent(percent);
        }
    }

    public void UpdateTimer(float secondsRemaining, bool suddenDeath)
    {
        if (timerLabel == null) return;

        int totalSec = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        int m = totalSec / 60;
        int s = totalSec % 60;

        if (suddenDeath)
        {
            timerLabel.text = $"SUDDEN DEATH! {m:00}:{s:00}";
            timerLabel.RemoveFromClassList("timer-warning");
            timerLabel.AddToClassList("timer-sudden-death");
        }
        else
        {
            timerLabel.RemoveFromClassList("timer-sudden-death");
            timerLabel.text = string.Format("{0:00}:{1:00}", m, s);

            if (totalSec <= 30)
            {
                timerLabel.AddToClassList("timer-warning");
            }
            else
            {
                timerLabel.RemoveFromClassList("timer-warning");
            }
        }
    }

    public void ShowVictory(string winnerText)
    {
        if (victoryLayer != null)
        {
            victoryLayer.style.display = DisplayStyle.Flex;
            if (victoryTitle != null) victoryTitle.text = winnerText;
        }
    }

    public void ShowDrawScreen()
    {
        if (drawLayer != null)
        {
            drawLayer.style.display = DisplayStyle.Flex;
        }
    }

    public void ShowSlackNotification(string sender, string message)
    {
        if (slackToast == null) return;

        if (slackSender != null) slackSender.text = sender;
        if (slackMessage != null) slackMessage.text = message;

        if (slackToastRoutine != null) StopCoroutine(slackToastRoutine);
        slackToastRoutine = StartCoroutine(SlackToastRoutine());
    }

    IEnumerator SlackToastRoutine()
    {
        slackToast.RemoveFromClassList("slack-toast-hidden");
        slackToast.AddToClassList("slack-toast-visible");

        yield return new WaitForSeconds(4.2f);

        slackToast.RemoveFromClassList("slack-toast-visible");
        slackToast.AddToClassList("slack-toast-hidden");
    }
}

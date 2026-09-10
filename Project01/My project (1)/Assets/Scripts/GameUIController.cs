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

    // Elements
    VisualElement p1Fill;
    VisualElement p2Fill;
    Label timerLabel;
    Label bossText;
    Label fightText;
    Label victoryTitle;
    Button startBtn;
    Button rematchBtn;

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

        // Query controls
        p1Fill = root.Q<VisualElement>("p1-health-fill");
        p2Fill = root.Q<VisualElement>("p2-health-fill");
        timerLabel = root.Q<Label>("timer-label");
        bossText = root.Q<Label>("boss-text");
        fightText = root.Q<Label>("fight-text");
        victoryTitle = root.Q<Label>("victory-title");
        startBtn = root.Q<Button>("start-btn");
        rematchBtn = root.Q<Button>("rematch-btn");

        // Bind clicks
        if (startBtn != null)
        {
            startBtn.clicked += OnStartClicked;
        }
        if (rematchBtn != null)
        {
            rematchBtn.clicked += OnRematchClicked;
        }

        // Default initial state
        ShowMenu(true);
        HideBossDialogue();
        ShowFightBanner(false);
        if (victoryLayer != null) victoryLayer.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        if (startBtn != null) startBtn.clicked -= OnStartClicked;
        if (rematchBtn != null) rematchBtn.clicked -= OnRematchClicked;
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

        if (suddenDeath)
        {
            timerLabel.text = "SUDDEN DEATH!";
            timerLabel.RemoveFromClassList("timer-warning");
            timerLabel.AddToClassList("timer-sudden-death");
        }
        else
        {
            timerLabel.RemoveFromClassList("timer-sudden-death");
            int totalSec = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            int m = totalSec / 60;
            int s = totalSec % 60;
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
}

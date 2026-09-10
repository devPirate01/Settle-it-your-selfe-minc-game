using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;
    public TextMeshProUGUI winnerText; // assign in inspector

    void Awake() => Instance = this;

    public void ShowWinner(string loserName)
    {
        // Figure out who won (the one who didn't die)
        string winner = loserName == "MP_Female_A1F Woman" ? "Player 2 Wins!" : "Player 1 Wins!";
        winnerText.text = winner;
        winnerText.gameObject.SetActive(true);

        // Show rematch button / prompt
        GameIntroManager.Instance?.OnGameOver();
    }
}

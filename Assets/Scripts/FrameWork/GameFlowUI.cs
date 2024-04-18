using UnityEngine;
using UnityEngine.UI;

public class GameFlowUI : MonoBehaviour
{
    Text GameOverText = null;
    Text WinnerText = null;
    void Start()
    {
        GameOverText = transform.Find("GameOverText").GetComponent<Text>();
        GameOverText.gameObject.SetActive(false);

        WinnerText = transform.Find("WinnerText").GetComponent<Text>();
        WinnerText.gameObject.SetActive(false);

        GameServices.GetGameState().OnGameOver += ShowGameResults;
    }

    void ShowGameResults(ETeam winner)
    {
        WinnerText.color = GameServices.GetTeamColor(winner);
        WinnerText.text = "Winner is " + winner.ToString() + " team";

        GameOverText.gameObject.SetActive(true);
        WinnerText.gameObject.SetActive(true);

        Time.timeScale = 0f;
    }
}

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

public class FinalSummaryUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;

    void Start()
    {
        ShowFinalResults();
    }

    void ShowFinalResults()
    {
        var players = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetScore(p)).ToList();

        if (players.Count >= 1)
            p1ScoreText.text = $"{players[0].NickName}: {GetScore(players[0])} pts";
        if (players.Count >= 2)
            p2ScoreText.text = $"{players[1].NickName}: {GetScore(players[1])} pts";

        if (players.Count > 1 && GetScore(players[0]) == GetScore(players[1]))
            winnerNameText.text = "🏆 เสมอกัน! 🏆";
        else
            winnerNameText.text = $"🏆 {players[0].NickName} WIN! 🏆";
    }

    int GetScore(Player player)
    {
        if (player.CustomProperties.TryGetValue("Score", out object score))
            return (int)score;
        return 0;
    }

    public void OnClickBackToLobby()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("menu");
            }
            else
            {
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
    }
}
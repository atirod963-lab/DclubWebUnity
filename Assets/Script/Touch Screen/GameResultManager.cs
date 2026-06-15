using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class GameResultManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText; 

    public void EndGameAndShowResult()
    {
        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.isGameActive = false;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);


        Player[] players = PhotonNetwork.PlayerList;
        var sortedPlayers = players.OrderByDescending(p => GetPlayerScore(p)).ToList();


        string finalText = "--- สรุปผลคะแนน ---\n\n";

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Player p = sortedPlayers[i];
            int pScore = GetPlayerScore(p);
            finalText += $"อันดับ {i + 1}: {p.NickName} ทำได้ {pScore} คะแนน\n";
        }


        if (sortedPlayers.Count > 1)
        {
            if (GetPlayerScore(sortedPlayers[0]) == GetPlayerScore(sortedPlayers[1]))
            {
                finalText += "\n<color=yellow>ผลสรุป: เสมอกัน! (DRAW)</color>";
            }
            else
            {
                finalText += $"\n<color=green>ผู้ชนะคือ: {sortedPlayers[0].NickName} 🏆!</color>";
            }
        }
        else
        {

            finalText += "\n<color=yellow>จบเกม!</color>";
        }


        if (resultText != null) resultText.text = finalText;
    }


    private int GetPlayerScore(Player player)
    {
        if (player.CustomProperties.TryGetValue("Score", out object scoreObj))
        {
            return (int)scoreObj;
        }
        return 0;
    }
}
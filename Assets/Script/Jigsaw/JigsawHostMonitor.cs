using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections.Generic;

public class JigsawHostMonitor : MonoBehaviourPunCallbacks
{
    [Header("📊 UI ฝั่งทีม 1 (สีเขียว)")]
    public TextMeshProUGUI t1RoundText;
    public TextMeshProUGUI t1ProgressText;
    public RectTransform t1PuzzleContainer;

    [Header("📊 UI ฝั่งทีม 2 (สีแดง)")]
    public TextMeshProUGUI t2RoundText;
    public TextMeshProUGUI t2ProgressText;
    public RectTransform t2PuzzleContainer;

    [Header("🖼️ ชื่อไฟล์ Sprite Sheet ใน Resources")]
    public string[] roundSpriteSheetNames = new string[] {
        "Galactic pink Multi",
        "Galactic blue Multi",
        "Galactic green Multi"
    };

    private List<Image> t1PieceImages = new List<Image>();
    private List<Image> t2PieceImages = new List<Image>();

    private int t1CurrentRound = 0;
    private int t2CurrentRound = 0;

    void Start()
    {
        if (t1PuzzleContainer != null && t1PuzzleContainer.GetComponent<Image>() != null)
            t1PuzzleContainer.GetComponent<Image>().enabled = false;
        if (t2PuzzleContainer != null && t2PuzzleContainer.GetComponent<Image>() != null)
            t2PuzzleContainer.GetComponent<Image>().enabled = false;

        if (PhotonNetwork.CurrentRoom != null) UpdateHostUI(PhotonNetwork.CurrentRoom.CustomProperties);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        UpdateHostUI(propertiesThatChanged);
    }

    void UpdateHostUI(Hashtable props)
    {
        if (props.ContainsKey("T1Round"))
        {
            int round = (int)props["T1Round"];
            if (t1RoundText != null) t1RoundText.text = $"ภาพที่: {round} / 3";

            if (round != t1CurrentRound)
            {
                t1CurrentRound = round;
                GeneratePuzzleGrid(t1PuzzleContainer, t1PieceImages, round);
            }
        }
        if (props.ContainsKey("T1PiecesArray")) 
        {
            RevealPieces(t1PieceImages, props["T1PiecesArray"].ToString());
        }
        if (props.ContainsKey("T1Progress"))
        {
            string p = props["T1Progress"].ToString();
            if (t1ProgressText != null) t1ProgressText.text = p;
            if (p == "FINISH!") GoToSummaryScene();
        }

        if (props.ContainsKey("T2Round"))
        {
            int round = (int)props["T2Round"];
            if (t2RoundText != null) t2RoundText.text = $"ภาพที่: {round} / 3";

            if (round != t2CurrentRound)
            {
                t2CurrentRound = round;
                GeneratePuzzleGrid(t2PuzzleContainer, t2PieceImages, round);
            }
        }
        if (props.ContainsKey("T2PiecesArray"))
        {
            RevealPieces(t2PieceImages, props["T2PiecesArray"].ToString());
        }
        if (props.ContainsKey("T2Progress"))
        {
            string p = props["T2Progress"].ToString();
            if (t2ProgressText != null) t2ProgressText.text = p;
            if (p == "FINISH!") GoToSummaryScene();
        }
    }

    void GeneratePuzzleGrid(RectTransform container, List<Image> pieceList, int round)
    {
        if (container == null || round <= 0 || round > roundSpriteSheetNames.Length) return;

        foreach (var img in pieceList) { if (img != null) Destroy(img.gameObject); }
        pieceList.Clear();

        Sprite[] slices = Resources.LoadAll<Sprite>(roundSpriteSheetNames[round - 1]);
        int totalPieces = (round == 1) ? 3 : (round == 2) ? 6 : 9;

        if (slices == null || slices.Length < totalPieces) return;

        int rows = Mathf.CeilToInt((float)totalPieces / 3f);

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = Vector2.zero;
            grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.LowerLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
        }
        grid.cellSize = new Vector2(container.rect.width / 3f, container.rect.height / rows);

        for (int i = 0; i < totalPieces; i++)
        {
            GameObject pieceObj = new GameObject($"Piece_{i}");
            pieceObj.transform.SetParent(container, false);

            Image img = pieceObj.AddComponent<Image>();
            img.sprite = slices[i];

            img.color = new Color(0, 0, 0, 0.2f);
            pieceList.Add(img);
        }
    }

    void RevealPieces(List<Image> pieceList, string placedPiecesStr)
    {
        if (string.IsNullOrEmpty(placedPiecesStr)) return;

        string[] indices = placedPiecesStr.Split(',');
        foreach (string idxStr in indices)
        {
            if (int.TryParse(idxStr, out int idx))
            {
                if (idx >= 0 && idx < pieceList.Count && pieceList[idx] != null)
                {
                    pieceList[idx].color = new Color(1f, 1f, 1f, 1f);
                }
            }
        }
    }

    void GoToSummaryScene() { if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("SummaryScene"); }
}
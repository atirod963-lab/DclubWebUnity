using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Game Mode Settings")]
    [Tooltip("ติ๊กถูกถ้าฉากนี้เป็นเกมบาส (ของจะสุ่มโผล่บนจอแทนการร่วงจากฟ้า)")]
    public bool isBasketballMode = false;

    // ---------------------------------------------------------
    // ⏱️ [เพิ่มใหม่] ช่องปรับความเร็วของร่วง
    [Tooltip("เวลาที่ใช้ร่วงจากฟ้า (วินาที) ยิ่งเลขเยอะ ยิ่งตกช้า")]
    public float foodFallDuration = 0.8f;
    // ---------------------------------------------------------

    [Header("UI Views")]
    public GameObject playerViewPanel;
    public GameObject hostViewPanel;

    [Header("Host Score UI")]
    public TextMeshProUGUI greenScoreText;
    public TextMeshProUGUI redScoreText;

    [Header("Tug of War UI (หลอดดึงเย่อ)")]
    public Slider tugOfWarSlider;

    [Header("Host Fake Visuals (ตัวหลอก)")]
    public GameObject[] greenTeamPopUps;
    public GameObject[] redTeamPopUps;

    private int currentGreenScore = 0;
    private int currentRedScore = 0;

    public bool isGameActive = true;
    private bool isHost = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (greenTeamPopUps != null)
        {
            foreach (GameObject obj in greenTeamPopUps)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (redTeamPopUps != null)
        {
            foreach (GameObject obj in redTeamPopUps)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (!PhotonNetwork.InRoom) return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            isHost = (role == "Spectator");

            if (playerViewPanel != null) playerViewPanel.SetActive(!isHost);
            if (hostViewPanel != null) hostViewPanel.SetActive(isHost);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalGreenScore"))
            currentGreenScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalGreenScore"];

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalRedScore"))
            currentRedScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalRedScore"];

        UpdateScoreUI();
    }

    public void AddScoreToMyTeam()
    {
        if (!isGameActive || !PhotonNetwork.InRoom) return;

        string myTeam = "None";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        if (myTeam == "Green" || myTeam == "Red")
        {
            photonView.RPC("RPC_AddScoreInstant", RpcTarget.All, myTeam);
        }
    }

    [PunRPC]
    void RPC_AddScoreInstant(string team)
    {
        if (team == "Green")
        {
            currentGreenScore++;
            StartCoroutine(ScorePopTrick(greenScoreText, Color.green));

            if (isHost && greenTeamPopUps != null && greenTeamPopUps.Length > 0)
            {
                int randomIndex = Random.Range(0, greenTeamPopUps.Length);
                if (greenTeamPopUps[randomIndex] != null)
                    StartCoroutine(ShowFakePopUp(greenTeamPopUps[randomIndex], true));
            }
        }
        else if (team == "Red")
        {
            currentRedScore++;
            StartCoroutine(ScorePopTrick(redScoreText, Color.red));

            if (isHost && redTeamPopUps != null && redTeamPopUps.Length > 0)
            {
                int randomIndex = Random.Range(0, redTeamPopUps.Length);
                if (redTeamPopUps[randomIndex] != null)
                    StartCoroutine(ShowFakePopUp(redTeamPopUps[randomIndex], false));
            }
        }

        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable hash = new Hashtable();
            hash.Add("GlobalGreenScore", currentGreenScore);
            hash.Add("GlobalRedScore", currentRedScore);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }

    public void SubtractScoreToMyTeam()
    {
        if (!isGameActive || !PhotonNetwork.InRoom) return;

        string myTeam = "None";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        if (myTeam == "Green" || myTeam == "Red")
        {
            photonView.RPC("RPC_SubtractScoreInstant", RpcTarget.All, myTeam);
        }
    }

    [PunRPC]
    void RPC_SubtractScoreInstant(string team)
    {
        if (team == "Green")
        {
            currentGreenScore--;
            StartCoroutine(ScorePopTrick(greenScoreText, Color.gray));
        }
        else if (team == "Red")
        {
            currentRedScore--;
            StartCoroutine(ScorePopTrick(redScoreText, Color.gray));
        }

        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable hash = new Hashtable();
            hash.Add("GlobalGreenScore", currentGreenScore);
            hash.Add("GlobalRedScore", currentRedScore);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }

    void UpdateScoreUI()
    {
        if (greenScoreText != null) greenScoreText.text = "Score: " + currentGreenScore;
        if (redScoreText != null) redScoreText.text = "Score: " + currentRedScore;

        if (tugOfWarSlider != null)
        {
            float gScore = Mathf.Max(0, currentGreenScore);
            float rScore = Mathf.Max(0, currentRedScore);
            float total = gScore + rScore;

            if (total == 0)
            {
                tugOfWarSlider.value = 0.5f;
            }
            else
            {
                tugOfWarSlider.value = rScore / total;
            }
        }
    }

    IEnumerator ScorePopTrick(TextMeshProUGUI scoreText, Color flashColor)
    {
        if (scoreText == null) yield break;

        Vector3 originalScale = scoreText.transform.localScale;
        scoreText.color = flashColor;

        if (!isHost)
        {
            scoreText.transform.localScale = originalScale * 1.5f;
        }

        yield return new WaitForSeconds(0.15f);

        scoreText.transform.localScale = originalScale;
        scoreText.color = Color.white;
    }

    IEnumerator ShowFakePopUp(GameObject popUpObj, bool isGreenTeam)
    {
        popUpObj.SetActive(false);
        popUpObj.SetActive(true);

        RectTransform rect = popUpObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(250f, 250f);
        }

        float randomX = isGreenTeam ? Random.Range(-400f, -100f) : Random.Range(100f, 400f);

        float startY;
        float endY;
        float animDuration;

        if (isBasketballMode)
        {
            startY = Random.Range(-300f, 300f);
            endY = startY;
            animDuration = 0f;
        }
        else
        {
            startY = 1000f;
            endY = Random.Range(-150f, 200f);
            // 🚀 [ปรับปรุงใหม่] ดึงค่าเวลาจากตัวแปรด้านบนมาใช้แทนเลขตายตัว
            animDuration = foodFallDuration;
        }

        Vector2 startPos = new Vector2(randomX, startY);
        Vector2 endPos = new Vector2(randomX, endY);

        if (rect != null) rect.anchoredPosition = startPos;

        Vector3 baseScale = Vector3.one;
        popUpObj.transform.localScale = baseScale;

        float timer = 0f;
        while (timer < animDuration)
        {
            timer += Time.deltaTime;
            if (rect != null)
            {
                float t = timer / animDuration;
                float ease = t * t;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);
            }
            yield return null;
        }
        if (rect != null) rect.anchoredPosition = endPos;

        float popMultiplier = 2.0f;
        popUpObj.transform.localScale = baseScale * popMultiplier;

        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.deltaTime;
            popUpObj.transform.localScale = Vector3.Lerp(baseScale * popMultiplier, baseScale, timer / 0.15f);
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);
        popUpObj.SetActive(false);
    }
}
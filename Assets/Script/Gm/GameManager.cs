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
    public bool isBasketballMode = false;
    public float foodFallDuration = 0.8f;

    [Header("UI Views")]
    public GameObject playerViewPanel;
    public GameObject hostViewPanel;
    public GameObject playerPersonalScoreUI;

    [Header("Host Score UI")]
    public TextMeshProUGUI greenScoreText;
    public TextMeshProUGUI redScoreText;

    [Header("Tug of War UI (หลอดดึงเย่อ)")]
    public Slider tugOfWarSlider;

    [Header("Host Fake Visuals (ตัวหลอก)")]
    public GameObject[] greenTeamPopUps;
    public GameObject[] redTeamPopUps;

    [Header("Floating Score Prefabs (Host)")]
    public GameObject plusScorePrefab;
    public GameObject minusScorePrefab;

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

        if (!PhotonNetwork.InRoom)
        {
            isHost = false;
            if (playerViewPanel != null) playerViewPanel.SetActive(true);
            if (hostViewPanel != null) hostViewPanel.SetActive(false);

            if (playerPersonalScoreUI != null) playerPersonalScoreUI.SetActive(true);

            if (TouchManager2D.Instance != null)
            {
                currentGreenScore = TouchManager2D.Instance.score;
            }
            UpdateScoreUI();

            return;
        }

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            isHost = (role == "Spectator");

            if (playerViewPanel != null) playerViewPanel.SetActive(!isHost);
            if (hostViewPanel != null) hostViewPanel.SetActive(isHost);

            if (playerPersonalScoreUI != null) playerPersonalScoreUI.SetActive(!isHost);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalGreenScore"))
            currentGreenScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalGreenScore"];

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalRedScore"))
            currentRedScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalRedScore"];

        UpdateScoreUI();
        SyncScoreToLocalTouchManager();
    }

    public void AddScoreToMyTeam()
    {
        if (!isGameActive) return;

        if (!PhotonNetwork.InRoom)
        {
            currentGreenScore++;
            UpdateScoreUI();
            if (TouchManager2D.Instance != null)
            {
                TouchManager2D.Instance.score = currentGreenScore;
                TouchManager2D.Instance.UpdateScoreUI();
            }
            return;
        }

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

        // 🌟 [เพิ่มระบบเสกตัวเลข +1 บนหน้า Host]
        if (isHost && plusScorePrefab != null && Camera.main != null)
        {
            // สุ่มเกิดฝั่งซ้าย (0.1 ถึง 0.4) สำหรับทีมเขียว และฝั่งขวา (0.6 ถึง 0.9) สำหรับทีมแดง
            float randomX = (team == "Green") ? Random.Range(0.1f, 0.4f) : Random.Range(0.6f, 0.9f);
            float randomY = Random.Range(0.3f, 0.7f);

            // แปลงพิกัดจากหน้าจอให้เป็นพิกัดโลก (World Space)
            Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, Mathf.Abs(Camera.main.transform.position.z)));

            GameObject floatingText = Instantiate(plusScorePrefab, spawnPos, Quaternion.identity);
            TextMeshPro tmpro = floatingText.GetComponent<TextMeshPro>();
            if (tmpro != null)
            {
                // เปลี่ยนสีตามทีม
                tmpro.color = (team == "Green") ? Color.green : Color.red;
            }
        }

        UpdateScoreUI();
        SyncScoreToLocalTouchManager();

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
        if (!isGameActive) return;

        if (!PhotonNetwork.InRoom)
        {
            currentGreenScore--;
            UpdateScoreUI();
            if (TouchManager2D.Instance != null)
            {
                TouchManager2D.Instance.score = currentGreenScore;
                TouchManager2D.Instance.UpdateScoreUI();
            }
            return;
        }

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

        // 🌟 [เพิ่มระบบเสกตัวเลข -1 บนหน้า Host]
        if (isHost && minusScorePrefab != null && Camera.main != null)
        {
            // สุ่มเกิดฝั่งซ้าย (0.1 ถึง 0.4) สำหรับทีมเขียว และฝั่งขวา (0.6 ถึง 0.9) สำหรับทีมแดง
            float randomX = (team == "Green") ? Random.Range(0.1f, 0.4f) : Random.Range(0.6f, 0.9f);
            float randomY = Random.Range(0.3f, 0.7f);

            // แปลงพิกัดจากหน้าจอให้เป็นพิกัดโลก (World Space)
            Vector3 spawnPos = Camera.main.ViewportToWorldPoint(new Vector3(randomX, randomY, Mathf.Abs(Camera.main.transform.position.z)));

            GameObject floatingText = Instantiate(minusScorePrefab, spawnPos, Quaternion.identity);
            TextMeshPro tmpro = floatingText.GetComponent<TextMeshPro>();
            if (tmpro != null)
            {
                // เปลี่ยนสีตามทีม
                tmpro.color = (team == "Green") ? Color.green : Color.red;
            }
        }

        UpdateScoreUI();
        SyncScoreToLocalTouchManager();

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

    void SyncScoreToLocalTouchManager()
    {
        if (isHost || !PhotonNetwork.InRoom || TouchManager2D.Instance == null) return;

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            string myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
            if (myTeam == "Green")
            {
                TouchManager2D.Instance.score = currentGreenScore;
                TouchManager2D.Instance.UpdateScoreUI();
            }
            else if (myTeam == "Red")
            {
                TouchManager2D.Instance.score = currentRedScore;
                TouchManager2D.Instance.UpdateScoreUI();
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
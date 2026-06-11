using System.Collections;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public TextMeshProUGUI warningText;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject lobbySelectionPanel;
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        lobbySelectionPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);


        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    public void OnPlayButtonClick()
    {
        if (nameInputField == null || string.IsNullOrWhiteSpace(nameInputField.text))
        {
            if (warningText != null) StartCoroutine(ShowWarningRoutine());
            return;
        }

        mainMenuPanel.SetActive(false);
        lobbySelectionPanel.SetActive(true);
    }

    IEnumerator ShowWarningRoutine()
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        warningText.gameObject.SetActive(false);
    }


    public void OnCreateButtonClick()
    {
        lobbySelectionPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    public void OnJoinButtonClick()
    {
        lobbySelectionPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }

    public void OnBackToLobbyClick()
    {
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        lobbySelectionPanel.SetActive(true);
    }

    public void OnBackToMainMenuClick()
    {
        lobbySelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
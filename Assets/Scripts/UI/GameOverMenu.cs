using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameOverMenu : Menu
{
    [SerializeField] private TMP_Text _headerText;
    [SerializeField] private TMP_Text _loreText;

    private string _investigatorWinText = string.Empty;
    private string _ghostWinText = string.Empty;

    protected override void OnStart()
    {
        GetUiTextsFromResources();

        _headerText.text = string.Empty;
        _loreText.text = string.Empty;

        LobbyNotifier.OnSceneReady.AddListener(SetupGameManagerEvents);
    }

    private void SetupGameManagerEvents()
    {
        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool win)
    {
        _headerText.text = win ? "Вы победили!" : "Вы проиграли!";

        var role = PlayerData.Local.Role;

        string loreMessage = (role, win) switch
        {
            (PlayerRole.Investigator, true) or (PlayerRole.Ghost, false) => _investigatorWinText,
            (PlayerRole.Ghost, true) or (PlayerRole.Investigator, false) => _ghostWinText,
            _ => throw new InvalidOperationException("Unsupported PlayerRole value.")
        };

        _loreText.text = loreMessage;

        Activate();

        Cursor.lockState = CursorLockMode.None;
    }

    public void LeaveGame()
    {
        _ = SessionManager.Instance.LeaveSession();
    }

    private void GetUiTextsFromResources()
    {
        TextAsset uiTextsResource = Resources.Load<TextAsset>("UiTexts");

        if (uiTextsResource == null)
        {
            Debug.LogError("UiTexts resource file was not found.");
            return;
        }

        UiTextCollection uiTextCollection = null;

        try
        {
            uiTextCollection = JsonUtility.FromJson<UiTextCollection>(uiTextsResource.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to deserialize Notes resource file: {ex.Message}");
        }

        _investigatorWinText = uiTextCollection.InvestigatorWinText;
        _ghostWinText = uiTextCollection.GhostWinText;
    }
}

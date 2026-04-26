using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateGameMenu : Menu
{
    [SerializeField] private Toggle _publicGameToggle;
    [SerializeField] private Button _createGameButton;
    [SerializeField] private StatusText _gameStatusText;
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private Button _copyJoinCodeButton;
    [SerializeField] private Button _cancelButton;

    private bool _isSessionRunning = false;

    protected override void OnStart()
    {
        Cursor.lockState = CursorLockMode.None;

        OnActivated.AddListener(() =>
        {
            _publicGameToggle.isOn = false;
            _joinCodeInputField.text = string.Empty;
            _gameStatusText.SetMessage(string.Empty);
            UnlockUI();

            if (LobbyNetworkManager.Instance.DEBUG_MODE)
            {
                _publicGameToggle.isOn = true;
                CreateGame();
            }
        });

        LobbyNetworkManager.OnServerAllPlayersReady.AddListener(OnLastPlayerJoined);
    }

    public void CreateGame()
    {
        LockUI();
        _cancelButton.interactable = true;

        _gameStatusText.SetMessage("Создание игры...");

        if (SessionManager.Instance.IsSignedIn)
            CreateGameAuthorized();
        else
            SessionManager.OnSignedIn.AddListener(CreateGameAuthorized);
    }

    private async void CreateGameAuthorized()
    {
        SessionManager.OnSignedIn.RemoveListener(CreateGameAuthorized);

        _cancelButton.interactable = false;

        bool isPublic = _publicGameToggle.isOn;

        var startSessionStatus = isPublic
            ? await SessionManager.Instance.StartPublicSessionAsHost()
            : await SessionManager.Instance.StartPrivateSessionAsHost();

        if (startSessionStatus is StartSessionStatus.Failed)
        {
            UnlockUI();
            _gameStatusText.SetError("Не удалось создать игру.");
            return;
        }

        _isSessionRunning = true;

        _joinCodeInputField.text = SessionManager.Instance.ActiveSession.Code;
        _gameStatusText.SetSuccess("Успех! Ожидание второго игрока...");

        UnlockUI();
    }

    private void OnLastPlayerJoined()
    {
        _gameStatusText.SetMessage("Создание лобби...");
    }

    public async void Cancel()
    {
        SessionManager.OnSignedIn.RemoveListener(CreateGameAuthorized);

        LockUI();

        if (!_isSessionRunning)
        {
            Deactivate();
            return;
        }

        _gameStatusText.SetMessage("Отмена...");

        var stopSessionStatus = await SessionManager.Instance.StopSession();

        if (stopSessionStatus is StopSessionStatus.Failed)
        {
            UnlockUI();
            _gameStatusText.SetError("Произошла ошибка при отмене создания игры.");
            return;
        }

        _isSessionRunning = false;

        _gameStatusText.SetSuccess("Создание игры отменено.");
        _joinCodeInputField.text = string.Empty;

        Deactivate();
    }

    private void LockUI()
    {
        _publicGameToggle.interactable = false;
        _createGameButton.interactable = false;
        _copyJoinCodeButton.interactable = false;
        _cancelButton.interactable = false;
    }

    private void UnlockUI()
    {
        _publicGameToggle.interactable = !_isSessionRunning;
        _createGameButton.interactable = !_isSessionRunning;
        _copyJoinCodeButton.interactable = !string.IsNullOrEmpty(_joinCodeInputField.text);
        _cancelButton.interactable = true;
    }
}

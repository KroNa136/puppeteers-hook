using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinGameMenu : Menu
{
    [SerializeField] private Button _joinRandomGameButton;
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private Button _pasteJoinCodeButton;
    [SerializeField] private Button _joinGameByCodeButton;
    [SerializeField] private StatusText _joinStatusText;
    [SerializeField] private Button _cancelButton;

    private bool _joinedSession = false;

    protected override void OnStart()
    {
        Cursor.lockState = CursorLockMode.None;

        OnActivated.AddListener(() =>
        {
            _joinCodeInputField.text = string.Empty;
            _joinStatusText.SetMessage(string.Empty);
            UnlockUI();

            if (LobbyNetworkManager.Instance.DEBUG_MODE)
            {
                JoinRandomGame();
            }
        });

        LobbyNetworkManager.OnClientDisconnected.AddListener(OnDisconnectedFromGame);
    }

    public void JoinRandomGame()
    {
        LockUI();
        _cancelButton.interactable = true;

        _joinStatusText.SetMessage("Поиск игры...");

        if (SessionManager.Instance.IsSignedIn)
            JoinRandomGameAuthorized();
        else
            SessionManager.OnSignedIn.AddListener(JoinRandomGameAuthorized);
    }

    private async void JoinRandomGameAuthorized()
    {
        SessionManager.OnSignedIn.RemoveListener(JoinRandomGameAuthorized);

        _cancelButton.interactable = false;

        var (findSessionsStatus, sessions) = await SessionManager.Instance.FindSessions();

        if (findSessionsStatus is FindSessionsStatus.Failed)
        {
            UnlockUI();
            _joinStatusText.SetError("Ошибка поиска. Повторите попытку позже.");
            return;
        }

        if (sessions.Count == 0)
        {
            UnlockUI();
            _joinStatusText.SetMessage("Публичных игр сейчас нет. Создайте свою игру или повторите поиск позже.");
            return;
        }

        _joinStatusText.SetMessage("Подключение...");

        var joinSessionStatus = await SessionManager.Instance.JoinSession(sessions[0]);

        if (joinSessionStatus is JoinSessionStatus.SessionIsFull)
        {
            UnlockUI();
            _joinStatusText.SetError("! Найдена заполненная игра !");
            return;
        }

        if (joinSessionStatus is JoinSessionStatus.NotFound or JoinSessionStatus.Failed)
        {
            UnlockUI();
            _joinStatusText.SetError("Ошибка подключения. Повторите попытку позже.");
            return;
        }

        _joinedSession = true;
        _joinStatusText.SetSuccess("Успех! Ожидание создания лобби...");
    }

    public void JoinGameByCode()
    {
        LockUI();
        _cancelButton.interactable = true;

        string code = _joinCodeInputField.text;

        if (code.ToCharArray().Length == 0)
        {
            UnlockUI();
            _joinStatusText.SetError("Введите код.");
            return;
        }

        if (code.ToCharArray().Length != 6)
        {
            UnlockUI();
            _joinStatusText.SetError("Код должен состоять из 6 символов.");
            return;
        }

        _joinStatusText.SetMessage("Подключение...");

        if (SessionManager.Instance.IsSignedIn)
            JoinGameByCodeAuthorized();
        else
            SessionManager.OnSignedIn.AddListener(JoinGameByCodeAuthorized);
    }

    private async void JoinGameByCodeAuthorized()
    {
        SessionManager.OnSignedIn.RemoveListener(JoinGameByCodeAuthorized);

        _cancelButton.interactable = false;

        var joinSessionStatus = await SessionManager.Instance.JoinSession(_joinCodeInputField.text);

        if (joinSessionStatus is JoinSessionStatus.SessionIsFull)
        {
            UnlockUI();
            _joinStatusText.SetError("В этой игре уже есть второй игрок.");
            return;
        }

        if (joinSessionStatus is JoinSessionStatus.NotFound)
        {
            UnlockUI();
            _joinStatusText.SetError("Игра с таким кодом не найдена.");
            return;
        }

        if (joinSessionStatus is JoinSessionStatus.Failed)
        {
            UnlockUI();
            _joinStatusText.SetError("Ошибка подключения. Повторите попытку позже.");
            return;
        }

        _joinedSession = true;
        _joinStatusText.SetSuccess("Успех! Ожидание создания лобби...");
    }

    private void OnDisconnectedFromGame()
    {
        UnlockUI();
        _joinStatusText.SetError("Сервер разорвал соединение.");
        _joinedSession = false;
    }

    public void Cancel()
    {
        if (_joinedSession)
        {
            Debug.LogWarning("Attempted to cancel joining a game when already joined.");
            return;
        }

        SessionManager.OnSignedIn.RemoveListener(JoinRandomGameAuthorized);
        SessionManager.OnSignedIn.RemoveListener(JoinGameByCodeAuthorized);

        LockUI();
        Deactivate();
    }

    private void LockUI()
    {
        _joinRandomGameButton.interactable = false;
        _joinCodeInputField.readOnly = true;
        _pasteJoinCodeButton.interactable = false;
        _joinGameByCodeButton.interactable = false;
        _cancelButton.interactable = false;
    }

    private void UnlockUI()
    {
        _joinRandomGameButton.interactable = true;
        _joinCodeInputField.readOnly = false;
        _pasteJoinCodeButton.interactable = true;
        _joinGameByCodeButton.interactable = true;
        _cancelButton.interactable = true;
    }
}

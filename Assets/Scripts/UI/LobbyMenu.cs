using Mirror;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenu : Menu
{
    [Scene][SerializeField] private string _menuScene;

    [Space]

    [SerializeField] private StatusText _lobbyStatusText;
    [SerializeField] private LobbyPlayerCard[] _lobbyPlayerCards;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _readyButton;

    [Space]

    [SerializeField] private float _readyTimeout = 5f;

    private LobbyPlayer _localPlayer;
    private bool _readyResponse = false;
    private string _disconnectReason = string.Empty;

    protected override void OnStart()
    {
        _lobbyStatusText.SetMessage("Ожидание готовности всех игроков...");
        _quitButton.interactable = true;
        _readyButton.interactable = false;

        LobbyNetworkManager.OnClientSceneChangedAndLoaded.AddListener(SetupPlayerCards);
        LobbyNetworkManager.OnClientDisconnected.AddListener(OnDisconnectedByServer);
        LobbyNotifier.OnAllLobbyPlayersSpawned.AddListener(OnAllLobbyPlayersSpawned);
    }

    private void SetupPlayerCards()
    {
        _lobbyPlayerCards[0].SetPlayer("Вы");
        _lobbyPlayerCards[1].SetPlayer("Оппонент");
    }

    public async void Quit()
    {
        LockUI();

        _lobbyStatusText.SetMessage("Выход...");

        LobbyNetworkManager.OnClientDisconnected.RemoveListener(OnDisconnectedByServer);

        var leaveSessionStatus = await SessionManager.Instance.LeaveSession();

        if (leaveSessionStatus is LeaveSessionStatus.Failed)
        {
            LobbyNetworkManager.OnClientDisconnected.AddListener(OnDisconnectedByServer);
            UnlockUI();
            _lobbyStatusText.SetError("Произошла ошибка при попытке покинуть игру.");
            return;
        }

        GoToMenuScene();
    }

    public void Ready()
    {
        if (_localPlayer == null)
            return;

        _readyButton.interactable = false;

        _localPlayer.CmdPlayerReady();
        _ = StartCoroutine(ShowErrorIfNotReadyAfterTimeout());
    }

    private IEnumerator ShowErrorIfNotReadyAfterTimeout()
    {
        yield return new WaitForSeconds(_readyTimeout);

        if (_readyResponse)
            yield break;

        _readyButton.interactable = true;
        _lobbyStatusText.SetError("Сервер не отвечает. Повторите попытку.");
    }

    private void OnAllLobbyPlayersSpawned()
    {
        _readyButton.interactable = true;

        var lobbyPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        foreach (var lobbyPlayer in lobbyPlayers)
        {
            if (lobbyPlayer.isLocalPlayer)
                _localPlayer = lobbyPlayer;

            lobbyPlayer.OnReady.AddListener(lobbyPlayer.isLocalPlayer ? OnLocalPlayerReady : OnOpponentReady);
        }

        if (LobbyNetworkManager.Instance.DEBUG_MODE)
            Ready();
    }

    private void OnLocalPlayerReady()
    {
        _readyResponse = true;
        _lobbyPlayerCards[0].SetPlayerReady();
    }

    private void OnOpponentReady()
    {
        _lobbyPlayerCards[1].SetPlayerReady();
    }

    private void OnDisconnectedByServer()
    {
        if (string.IsNullOrEmpty(_disconnectReason))
            _disconnectReason = "Сервер разорвал соединение.";

        // TODO: save the disconnect reason and show it in the menu scene
        GoToMenuScene();
    }

    private void GoToMenuScene() => SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(_menuScene));

    private void LockUI()
    {
        _quitButton.interactable = false;
        _readyButton.interactable = false;
    }

    private void UnlockUI()
    {
        _quitButton.interactable = true;
        _readyButton.interactable = true;
    }
}

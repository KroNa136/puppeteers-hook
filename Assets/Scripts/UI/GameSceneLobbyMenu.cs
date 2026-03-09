using Mirror;
using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneLobbyMenu : Menu
{
    [Scene][SerializeField] private string _menuScene;

    [Space]

    [SerializeField] private StatusText _gameStatusText;
    [SerializeField] private LobbyPlayerCard[] _lobbyPlayerCards;
    [SerializeField] private Slider _progressSlider;

    [Header("Ghost Character")]

    [SerializeField] private Sprite _ghostCharacterSprite;
    [SerializeField] private string _ghostCharacterName;
    [SerializeField] private string _ghostCharacterDescription;

    [Header("Investigator Character")]

    [SerializeField] private Sprite _investigatorCharacterSprite;
    [SerializeField] private string _investigatorCharacterName;
    [SerializeField] private string _investigatorCharacterDescription;

    [Header("Fallback Character")]

    [SerializeField] private Sprite _fallbackCharacterSprite;
    [SerializeField] private string _fallbackCharacterName;
    [SerializeField] private string _fallbackCharacterDescription;

    private string _disconnectReason = string.Empty;

    protected override void OnStart()
    {
        var playersData = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);

        foreach (var playerData in playersData)
            playerData.OnRoleAssigned.AddListener(playerData.IsLocal ? ShowLocalPlayerRole : ShowOpponentRole);

        _gameStatusText.SetMessage("Распределение ролей...");

        _progressSlider.minValue = 0f;
        _progressSlider.maxValue = 1f;
        _progressSlider.gameObject.SetActive(false);

        LobbyNetworkManager.OnClientSceneChangedAndLoaded.AddListener(SetupPlayerCards);
        LobbyNetworkManager.OnClientDisconnected.AddListener(OnDisconnectedByServer);
        LobbyNotifier.OnSceneReady.AddListener(SetupGameManagerEvents);
    }

    private void SetupGameManagerEvents()
    {
        GameManager.OnClientPlayerRolesAssigned.AddListener(OnAssignedPlayerRoles);
        GameManager.OnClientWorldGenerationCompleted.AddListener(OnWorldGenerationCompleted);
        GameManager.OnClientWorldReconstructionCompleted.AddListener(OnWorldReconstructionCompleted);
        GameManager.OnClientGhostPreparePhaseStarted.AddListener(OnGhostPreparePhaseStarted);
        GameManager.OnClientMainPhaseStarted.AddListener(OnMainPhaseStarted);
    }

    private void SetupPlayerCards()
    {
        _lobbyPlayerCards[0].SetPlayer("Вы");
        _lobbyPlayerCards[1].SetPlayer("Оппонент");
    }

    private void OnDisconnectedByServer()
    {
        if (string.IsNullOrEmpty(_disconnectReason))
            _disconnectReason = "Сервер разорвал соединение.";

        // TODO: save the disconnect reason and show it in the menu scene
        GoToMenuScene();
    }

    private void OnAssignedPlayerRoles()
    {
        _gameStatusText.SetMessage("Генерация мира...");

        _progressSlider.gameObject.SetActive(true);
        _progressSlider.value = 0f;
        GameManager.OnClientWorldGenerationProgressUpdated.AddListener(SetProgress);
    }

    private void ShowLocalPlayerRole(PlayerRole role) => ShowPlayerRole(local: true, role);
    private void ShowOpponentRole(PlayerRole role) => ShowPlayerRole(local: false, role);

    private void ShowPlayerRole(bool local, PlayerRole role)
    {
        var (sprite, name, description) = role switch
        {
            PlayerRole.Ghost        => (_ghostCharacterSprite, _ghostCharacterName, _ghostCharacterDescription),
            PlayerRole.Investigator => (_investigatorCharacterSprite, _investigatorCharacterName, _investigatorCharacterDescription),
            _                       => (_fallbackCharacterSprite, _fallbackCharacterName, _fallbackCharacterDescription)
        };

        var lobbyPlayerCard = _lobbyPlayerCards[local ? 0 : 1];
        lobbyPlayerCard.SetCharacter(sprite, name, description);
    }

    [Obsolete("This method was created for an older architecture and is no longer in use. Call ShowPlayerRole for each player individually instead.")]
    private void ShowPlayerRoles()
    {
        var lobbyPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

        /*
        foreach (var lobbyPlayer in lobbyPlayers)
        {
            var (sprite, name, description) = lobbyPlayer.Role switch
            {
                PlayerRole.Ghost        => (_ghostCharacterSprite, _ghostCharacterName, _ghostCharacterDescription),
                PlayerRole.Investigator => (_investigatorCharacterSprite, _investigatorCharacterName, _investigatorCharacterDescription),
                _                       => (_fallbackCharacterSprite, _fallbackCharacterName, _fallbackCharacterDescription)
            };
            
            var lobbyPlayerCard = _lobbyPlayerCards[lobbyPlayer.isLocalPlayer ? 0 : 1];
            lobbyPlayerCard.SetCharacter(sprite, name, description);
            
        }
        */

        /*
        FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None)
            .ToList()
            .ForEach(lobbyPlayer =>
                _lobbyPlayerCards[lobbyPlayer.isLocalPlayer ? 0 : 1].SetCharacter
                (
                    lobbyPlayer.Role switch
                    {
                        PlayerRole.Ghost => _ghostCharacterSprite,
                        PlayerRole.Investigator => _investigatorCharacterSprite,
                        _ => _fallbackCharacterSprite
                    },
                    lobbyPlayer.Role switch
                    {
                        PlayerRole.Ghost => _ghostCharacterName,
                        PlayerRole.Investigator => _investigatorCharacterName,
                        _ => _fallbackCharacterName
                    },
                    lobbyPlayer.Role switch
                    {
                        PlayerRole.Ghost => _ghostCharacterDescription,
                        PlayerRole.Investigator => _investigatorCharacterDescription,
                        _ => _fallbackCharacterDescription
                    }
                )
            );
        
        FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None).ToList().ForEach(lobbyPlayer => _lobbyPlayerCards[lobbyPlayer.isLocalPlayer ? 0 : 1].SetCharacter(lobbyPlayer.Role switch { PlayerRole.Ghost => _ghostCharacterSprite, PlayerRole.Investigator => _investigatorCharacterSprite, _ => _fallbackCharacterSprite }, lobbyPlayer.Role switch { PlayerRole.Ghost => _ghostCharacterName, PlayerRole.Investigator => _investigatorCharacterName, _ => _fallbackCharacterName }, lobbyPlayer.Role switch { PlayerRole.Ghost => _ghostCharacterDescription, PlayerRole.Investigator => _investigatorCharacterDescription, _ => _fallbackCharacterDescription }));
        */
    }

    private void SetProgress(float value)
    {
        _progressSlider.value = value;
    }

    private void OnWorldGenerationCompleted()
    {
        _gameStatusText.SetMessage("Реконструкция мира...");

        _progressSlider.value = 0f;
        GameManager.OnClientWorldReconstructionProgressUpdated.AddListener(SetProgress);
    }

    private void OnWorldReconstructionCompleted()
    {
        _gameStatusText.SetMessage("До начала игры: 0:00");

        _progressSlider.gameObject.SetActive(false);
        GameManager.OnClientTimerUpdated.AddListener(UpdateTimer);
    }

    private void UpdateTimer(int seconds)
    {
        int minutes = seconds / 60;
        int leftoverSeconds = seconds % 60;

        _gameStatusText.SetMessage($"До начала игры: {minutes}:{leftoverSeconds:00}");
    }

    private void OnGhostPreparePhaseStarted()
    {
        if (PlayerData.Local.Role is PlayerRole.Ghost)
            Deactivate();

        // TODO: make a ghost status text explaining why the investigator is waiting for so long
    }

    private void OnMainPhaseStarted()
    {
        if (PlayerData.Local.Role is PlayerRole.Investigator)
            Deactivate();
    }

    private void GoToMenuScene() => SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(_menuScene));
}

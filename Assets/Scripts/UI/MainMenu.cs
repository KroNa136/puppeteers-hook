using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MainMenu : Menu
{
    [SerializeField] private CreateGameMenu _createGameMenu;
    [SerializeField] private JoinGameMenu _joinGameMenu;
    [SerializeField] private SettingsMenu _settingsMenu;

    [SerializeField] private StatusText _gameStatusText;

    protected override void OnStart()
    {
        Cursor.lockState = CursorLockMode.None;

        SessionManager.OnFailedToSignIn.AddListener(OnFailedToSignIn);
    }

    private void OnFailedToSignIn()
    {
        _gameStatusText.SetError("Произошла ошибка при подключении к Unity Services. Проверьте соединение и перезапустите игру.");
    }

    public void CreateGame()
    {
        _createGameMenu.Activate();
    }

    public void JoinGame()
    {
        _joinGameMenu.Activate();
    }

    public void Settings()
    {
        _settingsMenu.Activate();
    }

    public void Quit()
    {
        Application.Quit();
    }
}

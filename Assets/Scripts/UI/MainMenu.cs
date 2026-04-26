using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MainMenu : Menu
{
    [SerializeField] private CreateGameMenu _createGameMenu;
    [SerializeField] private JoinGameMenu _joinGameMenu;
    [SerializeField] private SettingsMenu _settingsMenu;

    protected override void OnStart()
    {
        Cursor.lockState = CursorLockMode.None;
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

using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MainMenu : Menu
{
    [SerializeField] private Menu _createGameMenu;
    [SerializeField] private Menu _joinGameMenu;
    [SerializeField] private Menu _settingsMenu;

    protected override void OnStart() { }

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

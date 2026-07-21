using Mirror;
using UnityEngine;

public class PauseMenu : Menu
{
    [SerializeField] private SettingsMenu _settingsMenu;

    private PauseManager _pauseManager;

    private CursorLockMode _cursorLockStateBeforeActivation;

    protected override void OnDeactivate()
    {
        if (_settingsMenu.IsActive)
            _settingsMenu.Deactivate();
    }

    public void ActivateFrom(PauseManager pauseManager)
    {
        if (_settingsMenu.IsActive)
        {
            _settingsMenu.Deactivate();

            Cursor.lockState = _cursorLockStateBeforeActivation is not CursorLockMode.Locked
                ? _cursorLockStateBeforeActivation
                : CursorLockMode.Locked;

            return;
        }

        _pauseManager = pauseManager;

        _cursorLockStateBeforeActivation = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;

        Activate();
    }

    public void DeactivateFromPauseManager()
    {
        Deactivate();

        Cursor.lockState = _cursorLockStateBeforeActivation is not CursorLockMode.Locked
            ? _cursorLockStateBeforeActivation
            : CursorLockMode.Locked;
    }

    public void Continue()
    {
        if (_pauseManager != null)
            _pauseManager.Unpause();
        else
            Deactivate();
    }

    public void Settings()
    {
        _settingsMenu.Activate();
    }

    public void LeaveGame()
    {
        _ = SessionManager.Instance.LeaveSession();
    }
}

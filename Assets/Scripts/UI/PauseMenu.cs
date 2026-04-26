using Mirror;
using UnityEngine;

public class PauseMenu : Menu
{
    private PauseManager _pauseManager;

    private CursorLockMode _cursorLockStateBeforeActivation;

    protected override void OnActivate()
    {
        _cursorLockStateBeforeActivation = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
    }

    protected override void OnDeactivate()
    {
        Cursor.lockState = _cursorLockStateBeforeActivation is not CursorLockMode.Locked
            ? _cursorLockStateBeforeActivation
            : CursorLockMode.Locked;
    }

    public void ActivateFrom(PauseManager pauseManager)
    {
        _pauseManager = pauseManager;
        Activate();
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
        // TODO: Settings submenu
    }

    public void LeaveGame()
    {
        _ = SessionManager.Instance.LeaveSession();
    }
}

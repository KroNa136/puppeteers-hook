using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private PauseMenu _pauseMenu;
    private InputManager _inputManager;
    private GamePlayerMovement _gamePlayerMovement;
    private CameraController _cameraController;
    private FlashlightManager _flashlightManager;
    private InteractionManager _interactionManager;
    private GhostAbilityManager _ghostAbilityManager;

    private bool _canPause = true;

    private void Start()
    {
        _pauseMenu = GameObject.Find("Pause Menu").GetComponent<PauseMenu>();
        _ = TryGetComponent(out _inputManager);
        _ = TryGetComponent(out _gamePlayerMovement);
        _ = TryGetComponent(out _cameraController);
        _ = TryGetComponent(out _flashlightManager);
        _ = TryGetComponent(out _interactionManager);
        _ = TryGetComponent(out _ghostAbilityManager);

        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool _)
    {
        _canPause = false;

        if (_pauseMenu.TryGet(m => m.IsActive, out bool active) && active)
            _pauseMenu.Deactivate();
    }

    private void Update()
    {
        if (!_canPause || _pauseMenu == null)
            return;

        if (_inputManager.GetOrDefault(im => im.Pause))
        {
            if (_pauseMenu.IsActive)
                Unpause();
            else
                Pause();
        }
    }

    public void Pause()
    {
        _ = _pauseMenu.Bind((m, pauseManager) => m.ActivateFrom(pauseManager), this);
        _ = _gamePlayerMovement.Bind(gpm => gpm.CanBeControlledByPlayer = false);
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = false);
        _ = _flashlightManager.Bind(fm => fm.CanBeControlledByPlayer = false);
        _ = _interactionManager.Bind(im => im.CanBeControlledByPlayer = false);
        _ = _ghostAbilityManager.Bind(gam => gam.CanBeControlledByPlayer = false);
    }

    public void Unpause()
    {
        _ = _pauseMenu.Bind(m => m.Deactivate());
        _ = _gamePlayerMovement.Bind(gpm => gpm.CanBeControlledByPlayer = true);
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = true);
        _ = _flashlightManager.Bind(fm => fm.CanBeControlledByPlayer = true);
        _ = _interactionManager.Bind(im => im.CanBeControlledByPlayer = true);
        _ = _ghostAbilityManager.Bind(gam => gam.CanBeControlledByPlayer = true);
    }
}

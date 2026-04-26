using UnityEngine;

public class GhostAbilityManager : MonoBehaviour
{
    private GhostAbilityMenu _abilityMenu;
    private InputManager _inputManager;
    private CameraController _cameraController;

    public bool CanBeControlledByPlayer = true;

    private void Start()
    {
        _abilityMenu = GameObject.Find("Ghost Ability Menu").GetComponent<GhostAbilityMenu>();
        _ = TryGetComponent(out _inputManager);
        _ = TryGetComponent(out _cameraController);

        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool _)
    {
        CanBeControlledByPlayer = false;

        if (_abilityMenu.TryGet(m => m.IsActive, out bool active) && active)
            _abilityMenu.Deactivate();
    }

    private void Update()
    {
        if (!CanBeControlledByPlayer || _abilityMenu == null)
            return;

        if (_inputManager.GetOrDefault(im => im.Abilities))
        {
            if (_abilityMenu.IsActive)
                CloseAbilityMenu();
            else
                OpenAbilityMenu();
        }
    }

    public void OpenAbilityMenu()
    {
        _ = _abilityMenu.Bind((menu, ghostAbilityManager) => menu.ActivateFrom(ghostAbilityManager), this);
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = false);
    }

    public void CloseAbilityMenu()
    {
        _ = _abilityMenu.Bind(menu => menu.Deactivate());
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = true);
    }
}

using Mirror;
using UnityEngine;

public class NoteReader : NetworkBehaviour
{
    [SerializeField] private float _maxNoteCorruptionFraction = 0.25f;

    private NoteReadingMenu _noteReadingMenu;
    private GamePlayerMovement _gamePlayerMovement;
    private CameraController _cameraController;
    private FlashlightManager _flashlightManager;
    private InteractionManager _interactionManager;
    private SanityManager _sanityManager;

    public bool CanBeControlledByPlayer = true;

    private void Start()
    {
        _noteReadingMenu = GameObject.Find("Note Reading Menu").GetComponent<NoteReadingMenu>();
        _ = TryGetComponent(out _gamePlayerMovement);
        _ = TryGetComponent(out _cameraController);
        _ = TryGetComponent(out _flashlightManager);
        _ = TryGetComponent(out _interactionManager);
        _ = TryGetComponent(out _sanityManager);

        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool _)
    {
        CanBeControlledByPlayer = false;

        if (_noteReadingMenu.TryGet(m => m.IsActive, out bool active) && active)
            _noteReadingMenu.Deactivate();
    }

    public void OpenNoteReadingMenu(Note noteToRead)
    {
        string text = _sanityManager.TryGet(sm => sm.CurrentSanityFraction, out float sanityFraction)
            ? noteToRead.GetCorruptedText(corruptionFraction: _maxNoteCorruptionFraction * (1f - sanityFraction))
            : noteToRead.Text;

        _ = _noteReadingMenu.Bind((menu, noteReader, noteText) => menu.ActivateFromNoteReader(noteReader, noteText), this, text);
        _ = _gamePlayerMovement.Bind(gpm => gpm.CanBeControlledByPlayer = false);
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = false);
        _ = _flashlightManager.Bind(fm => fm.CanBeControlledByPlayer = false);
        _ = _interactionManager.Bind(im => im.CanBeControlledByPlayer = false);
    }

    public void CloseNoteReadingMenu()
    {
        _ = _noteReadingMenu.Bind(menu => menu.Deactivate());
        _ = _gamePlayerMovement.Bind(gpm => gpm.CanBeControlledByPlayer = true);
        _ = _cameraController.Bind(cc => cc.CanBeControlledByPlayer = true);
        _ = _flashlightManager.Bind(fm => fm.CanBeControlledByPlayer = true);
        _ = _interactionManager.Bind(im => im.CanBeControlledByPlayer = true);
    }
}

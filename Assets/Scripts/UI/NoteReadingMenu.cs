using Mirror;
using TMPro;
using UnityEngine;

public class NoteReadingMenu : Menu
{
    [SerializeField] private TMP_Text _noteText;

    private NoteReader _noteReader;

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

    public void ActivateFromNoteReader(NoteReader noteReader, string noteText)
    {
        _noteReader = noteReader;
        _noteText.text = noteText;
        Activate();
    }

    public void StopReading()
    {
        if (_noteReader != null)
            _noteReader.CloseNoteReadingMenu();
        else
            Deactivate();
    }
}

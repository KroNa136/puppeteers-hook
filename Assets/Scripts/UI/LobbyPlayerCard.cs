using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private Image _characterImage;
    [SerializeField] private TMP_Text _characterName;
    [SerializeField] private TMP_Text _characterDescription;
    [SerializeField] private GameObject _unknownCharacterPanel;
    [SerializeField] private StatusPanel _readyStatusPanel;

    [SerializeField] private bool _useReadyStatusPanel = true;

    private void Start()
    {
        _playerName.text = string.Empty;
        _characterImage.sprite = null;
        _characterName.text = string.Empty;
        _characterDescription.text = string.Empty;
        _unknownCharacterPanel.SetActive(true);

        if (_useReadyStatusPanel)
            _readyStatusPanel.SetDisabled("Œ∆»ƒ¿Õ»≈ »√–Œ ¿");
        else
            _readyStatusPanel.gameObject.SetActive(false);
    }

    public void SetPlayer(string name, bool isReady = false)
    {
        _playerName.text = name;

        if (_useReadyStatusPanel)
        {
            if (isReady)
                _readyStatusPanel.SetSuccess("√Œ“Œ¬");
            else
                _readyStatusPanel.SetError("Õ≈ √Œ“Œ¬");
        }
    }

    public void SetPlayerReady()
    {
        if (_useReadyStatusPanel)
            _readyStatusPanel.SetSuccess("√Œ“Œ¬");
    }

    public void ClearPlayer()
    {
        _playerName.text = string.Empty;

        if (_useReadyStatusPanel)
            _readyStatusPanel.SetDisabled("Œ∆»ƒ¿Õ»≈ »√–Œ ¿");
    }

    public void SetCharacter(Sprite sprite, string name, string description)
    {
        _characterImage.sprite = sprite;
        _characterName.text = name;
        _characterDescription.text = description;

        _unknownCharacterPanel.SetActive(false);
    }

    public void ClearCharacter()
    {
        _unknownCharacterPanel.SetActive(true);

        _characterImage.sprite = null;
        _characterName.text = string.Empty;
        _characterDescription.text = string.Empty;
    }
}

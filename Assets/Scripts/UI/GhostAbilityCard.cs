using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GhostAbilityCard : MonoBehaviour
{
    public UnityEvent OnActivated = new();

    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private StatusText _nameText;
    [SerializeField] private StatusText _descriptionText;
    [SerializeField] private StatusText _durationText;
    [SerializeField] private StatusText _cooldownText;

    [Space]

    [SerializeField] private Color _availableColor = new(r: 1f, g: 1f, b: 1f, a: 1f);
    [SerializeField] private Color _unavailableColor = new(r: 0.75f, g: 0.75f, b: 0.75f, a: 1f);

    private GhostAbility _linkedAbility;

    public void Initialize(GhostAbility ability)
    {
        _linkedAbility = ability;

        var data = ability.Data;
        string name = data.Name;
        string description = data.Description;
        string duration = $"Длительность: {(data.Duration > 0 ? $"{data.Duration} секунд" : "-")}";
        string cooldown = $"Восстановление: {data.Cooldown} секунд";

        _ = data.Icon.Bind((i, image) => image.sprite = i, _icon);

        bool isAvailable = ability.CanBeActivated && !ability.IsActivated && !ability.IsCoolingDown;

        if (isAvailable)
        {
            _button.interactable = true;
            _icon.color = _availableColor;
            _nameText.SetMessage(name);
            _descriptionText.SetMessage(description);
            _durationText.SetMessage(duration);
            _cooldownText.SetMessage(cooldown);
        }
        else
        {
            _button.interactable = false;
            _icon.color = _unavailableColor;
            _nameText.SetDisabled(name);
            _descriptionText.SetDisabled(description);
            _durationText.SetDisabled(duration);
            _cooldownText.SetDisabled(cooldown);
        }

        ability.OnCanBeActivatedChanged.AddListener(SetAvailability);
        ability.OnActivated.AddListener(SetUnavailable);
        ability.OnStopCooldown.AddListener(SetAvailable);
    }

    private void SetAvailability(bool canBeActivated)
    {
        if (!canBeActivated || _linkedAbility.IsActivated || _linkedAbility.IsCoolingDown)
            SetUnavailable();
        else
            SetAvailable();
    }

    private void SetAvailable()
    {
        _button.interactable = true;
        _icon.color = _availableColor;
        _nameText.SetMessage(_nameText.Text);
        _descriptionText.SetMessage(_descriptionText.Text);
        _durationText.SetMessage(_durationText.Text);
        _cooldownText.SetMessage(_cooldownText.Text);
    }

    private void SetUnavailable()
    {
        _button.interactable = false;
        _icon.color = _unavailableColor;
        _nameText.SetDisabled(_nameText.Text);
        _descriptionText.SetDisabled(_descriptionText.Text);
        _durationText.SetDisabled(_durationText.Text);
        _cooldownText.SetDisabled(_cooldownText.Text);
    }

    public void ActivateAbility()
    {
        if (_linkedAbility == null)
            return;

        _linkedAbility.CmdActivate();
        OnActivated.Invoke();
    }
}

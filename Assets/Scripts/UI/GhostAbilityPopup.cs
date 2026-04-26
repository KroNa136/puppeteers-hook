using UnityEngine;
using UnityEngine.UI;

public class GhostAbilityPopup : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private StatusText _timerText;

    [Space]

    [SerializeField] private Color _activatedColor = new(r: 1f, g: 1f, b: 1f, a: 1f);
    [SerializeField] private Color _coolingDownColor = new(r: 0.75f, g: 0.75f, b: 0.75f, a: 1f);

    private bool _isCoolingDown;

    public void Initialize(GhostAbility ability)
    {
        _isCoolingDown = false;

        var data = ability.Data;

        _ = data.Icon.Bind((i, image) => image.sprite = i, _icon);
        _icon.color = _activatedColor;

        SetTime(data.Duration);

        ability.OnTimerUpdated.AddListener(SetTime);
        ability.OnStartCooldown.AddListener(StartCooldown);
        ability.OnStopCooldown.AddListener(StopCooldown);
    }

    private void SetTime(int seconds)
    {
        int minutes = seconds / 60;
        int leftoverSeconds = seconds % 60;

        string time = $"{minutes}:{leftoverSeconds:00}";

        if (_isCoolingDown)
            _timerText.SetDisabled(time);
        else
            _timerText.SetMessage(time);
    }

    private void StartCooldown()
    {
        _isCoolingDown = true;

        _icon.color = _coolingDownColor;
        _timerText.SetDisabled(_timerText.Text);
    }

    private void StopCooldown()
    {
        Destroy(gameObject);
    }
}

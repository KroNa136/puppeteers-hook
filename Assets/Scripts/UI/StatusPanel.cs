using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusPanel : MonoBehaviour
{
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _errorColor = Color.red;
    [SerializeField] private Color _successColor = Color.green;
    [SerializeField] private Color _disabledColor = Color.gray;

    private Color _normalPanelColor;
    private Color _errorPanelColor;
    private Color _successPanelColor;
    private Color _disabledPanelColor;

    private Color _colorDifference = new(r: 0.2f, g: 0.2f, b: 0.2f);

    [Space]

    [SerializeField] private TMP_Text _text;
    [SerializeField] private Image _image;

    private void Start()
    {
        _normalPanelColor = _normalColor - _colorDifference;
        _errorPanelColor = _errorColor - _colorDifference;
        _successPanelColor = _successColor - _colorDifference;
        _disabledPanelColor = _disabledColor - _colorDifference;
    }

    public void SetMessage(string message) => SetStatus(_normalPanelColor, _normalColor, message);
    public void SetError(string message) => SetStatus(_errorPanelColor, _errorColor, message);
    public void SetSuccess(string message) => SetStatus(_successPanelColor, _successColor, message);
    public void SetDisabled(string message) => SetStatus(_disabledPanelColor, _disabledColor, message);

    private void SetStatus(Color panelColor, Color textColor, string message)
    {
        _image.color = panelColor;
        _text.color = textColor;
        _text.text = message;
    }
}

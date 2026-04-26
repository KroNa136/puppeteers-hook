using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : Menu
{
    [SerializeField] private RectTransform _canvas;

    [Space]

    [SerializeField] private StatusText _gameTimerText;

    [Space]

    [SerializeField] private GameObject _crosshair;

    [Space]

    [SerializeField] private GameObject _investigatorFlashlightChargeBar;
    [SerializeField] private Image _investigatorFlashlightChargeIcon;
    [SerializeField] private Slider _investigatorFlashlightChargeSlider;
    [SerializeField] private Image _investigatorFlashlightChargeFill;

    [Space]

    [SerializeField] private GameObject _investigatorStaminaBar;
    [SerializeField] private Image _investigatorStaminaIcon;
    [SerializeField] private Slider _investigatorStaminaSlider;
    [SerializeField] private Image _investigatorStaminaFill;

    [Space]

    [SerializeField] private Image _investigatorSanityIndicator;

    [Space]

    [SerializeField] private GameObject _investigatorAmuletIcon;

    [Space]

    [SerializeField] private GameObject _ghostStaminaBar;
    [SerializeField] private Slider _ghostStaminaSlider;

    [Space]

    [SerializeField] private GameObject _ghostDashChargeBar;
    [SerializeField] private Slider _ghostDashChargeSlider;

    [Space]

    [SerializeField] private GridLayoutGroup _ghostAbilityPopupsGrid;
    [SerializeField] private GameObject _ghostAbilityPopupPrefab;

    [Space]

    [SerializeField] private GameObject _compass;
    [SerializeField] private Slider _compassSlider;
    [SerializeField] private Image _compassIcon;
    [SerializeField] private GameObject _compassLeftIndicator;
    [SerializeField] private Image _compassLeftIndicatorIcon;
    [SerializeField] private GameObject _compassRightIndicator;
    [SerializeField] private Image _compassRightIndicatorIcon;

    [Space]

    [SerializeField] private Color _normalColor = new(r: 1f, g: 1f, b: 1f, a: 1f);
    [SerializeField] private Color _mutedColor = new(r: 0.75f, g: 0.75f, b: 0.75f, a: 1f);
    [SerializeField] private Color _criticalColor = new(r: 1f, g: 0f, b: 0f, a: 1f);

    protected override void OnStart()
    {
        GameManager.OnClientTimerUpdated.AddListener(SetGameTime);

        _crosshair.SetActive(false);
        _investigatorFlashlightChargeBar.SetActive(false);
        _investigatorStaminaBar.SetActive(false);
        _investigatorSanityIndicator.color = new Color(r: 1f, g: 1f, b: 1f, a: 0f);
        _investigatorAmuletIcon.SetActive(false);
        _ghostStaminaBar.SetActive(false);
        _ghostDashChargeBar.SetActive(false);
        _compass.SetActive(false);
    }

    private void SetGameTime(int seconds)
    {
        int minutes = seconds / 60;
        int leftoverSeconds = seconds % 60;

        _gameTimerText.SetMessage($"{minutes}:{leftoverSeconds:00}");
    }

    public void SetCrosshair(bool enabled)
    {
        _crosshair.SetActive(enabled);
    }

    public void InitializeInvestigatorFlashlightChargeBar(float maxFlashlightCharge)
    {
        _investigatorFlashlightChargeBar.SetActive(false);

        _investigatorFlashlightChargeSlider.minValue = 0f;
        _investigatorFlashlightChargeSlider.maxValue = maxFlashlightCharge;
        _investigatorFlashlightChargeSlider.value = maxFlashlightCharge;
    }

    public void SetInvestigatorFlashlightCharge(float flashlightCharge, float maxFlashlightCharge, bool isCriticalFlashlightCharge)
    {
        _investigatorFlashlightChargeBar.SetActive(flashlightCharge < maxFlashlightCharge);
        _investigatorFlashlightChargeSlider.value = flashlightCharge;

        Color uiColor = isCriticalFlashlightCharge ? _criticalColor : _normalColor;

        _investigatorFlashlightChargeIcon.color = uiColor;
        _investigatorFlashlightChargeFill.color = uiColor;
    }

    public void InitializeInvestigatorStaminaBar(float maxStamina)
    {
        _investigatorStaminaBar.SetActive(false);

        _investigatorStaminaSlider.minValue = 0f;
        _investigatorStaminaSlider.maxValue = maxStamina;
        _investigatorStaminaSlider.value = maxStamina;
    }

    public void SetInvestigatorStamina(float stamina, float maxStamina, bool isCriticalStamina)
    {
        _investigatorStaminaBar.SetActive(stamina < maxStamina);
        _investigatorStaminaSlider.value = stamina;

        Color uiColor = isCriticalStamina ? _criticalColor : _normalColor;

        _investigatorStaminaIcon.color = uiColor;
        _investigatorStaminaFill.color = uiColor;
    }

    public void SetInvestigatorSanity(float sanity, float maxSanity)
    {
        _investigatorSanityIndicator.color = new Color(r: 1f, g: 1f, b: 1f, a: 1f - (sanity / maxSanity));
    }

    public void SetInvestigatorAmulet(bool hasAmulet)
    {
        // TODO: effects, tutorial
        _investigatorAmuletIcon.SetActive(hasAmulet);
    }

    public void ShakeInvestigatorAmuletIcon()
    {
        if (!_investigatorAmuletIcon.activeInHierarchy)
            return;

        // TODO: animator and shaking animation
    }

    public void InitializeGhostStaminaBar(float maxStamina)
    {
        _ghostStaminaBar.SetActive(false);

        _ghostStaminaSlider.minValue = 0f;
        _ghostStaminaSlider.maxValue = maxStamina;
        _ghostStaminaSlider.value = maxStamina;
    }

    public void SetGhostStamina(float stamina, float maxStamina)
    {
        _ghostStaminaBar.SetActive(stamina < maxStamina);
        _ghostStaminaSlider.value = stamina;
    }

    public void InitializeGhostDashChargeBar(float dashChargeDuration)
    {
        _ghostDashChargeBar.SetActive(false);

        _ghostDashChargeSlider.minValue = 0f;
        _ghostDashChargeSlider.maxValue = dashChargeDuration;
        _ghostDashChargeSlider.value = 0f;
    }

    public void SetGhostDashCharge(float dashCharge)
    {
        _ghostDashChargeBar.SetActive(dashCharge > 0f);
        _ghostDashChargeSlider.value = dashCharge;
    }

    public void SpawnGhostAbilityPopup(GhostAbility ghostAbility)
    {
        var ghostAbilityPopup = Instantiate(_ghostAbilityPopupPrefab, _ghostAbilityPopupsGrid.transform).GetComponent<GhostAbilityPopup>();
        ghostAbilityPopup.Initialize(ghostAbility);
    }

    public void EnableCompass()
    {
        _compass.SetActive(true);
        _compassLeftIndicator.SetActive(false);
        _compassRightIndicator.SetActive(false);

        RectTransform sliderRectTransform = _compassSlider.GetComponent<RectTransform>();
        float sliderWidth = sliderRectTransform.rect.width;

        Transform current = sliderRectTransform;

        while (current != _canvas && current != null)
        {
            sliderWidth *= current.localScale.x;
            current = current.parent;
        }

        float screenWidthFraction = sliderWidth / _canvas.rect.width;
        float fieldOfView = screenWidthFraction * Camera.main.GetHorizontalFieldOfView();
        float halfFieldOfView = fieldOfView * 0.5f;

        _compassSlider.minValue = -halfFieldOfView;
        _compassSlider.maxValue = halfFieldOfView;
        _compassSlider.value = 0f;

        _compassIcon.color = _normalColor;
        _compassLeftIndicatorIcon.color = _normalColor;
        _compassRightIndicatorIcon.color = _normalColor;
    }

    public void UpdateCompass(float horizontalAngleToTarget, bool targetIsAtDifferentHeight)
    {
        if (!_compass.activeInHierarchy)
            return;

        _compassLeftIndicator.SetActive(horizontalAngleToTarget < _compassSlider.minValue);
        _compassRightIndicator.SetActive(horizontalAngleToTarget > _compassSlider.maxValue);

        _compassSlider.value = horizontalAngleToTarget;

        if (targetIsAtDifferentHeight)
        {
            _compassIcon.color = _mutedColor;
            _compassLeftIndicatorIcon.color = _mutedColor;
            _compassRightIndicatorIcon.color = _mutedColor;
        }
        else
        {
            _compassIcon.color = _normalColor;
            _compassLeftIndicatorIcon.color = _normalColor;
            _compassRightIndicatorIcon.color = _normalColor;
        }
    }

    public void DisableCompass()
    {
        _compass.SetActive(false);
    }

    /*
    protected override void OnStart()
    {
        if (PlayerData.Local == null)
        {
            Debug.LogWarning("Game HUD tried to start without a local Player Data set!");
            return;
        }

        var role = PlayerData.Local.Role;

        if (role is PlayerRole.None)
            PlayerData.Local.OnRoleAssigned.AddListener(SetupForRole);
        else
            SetupForRole(role);
    }
    
    private void SetupForRole(PlayerRole role)
    {
        _investigatorStaminaBar.SetActive(role is PlayerRole.Investigator);
        _ghostStaminaBar.SetActive(role is PlayerRole.Ghost);
        _ghostDashChargeBar.SetActive(role is PlayerRole.Ghost);
    }
    */
}

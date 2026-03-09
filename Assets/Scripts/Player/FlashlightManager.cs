using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlashlightManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Light lightComponent;
    [SerializeField] GameObject chargeBar;
    [SerializeField] Image chargeBarIcon;
    [SerializeField] Image chargeBarFill;
    [SerializeField] Slider slider;
    [SerializeField] PlayerAudioController audioController;

    [Header("Values")]

    [SerializeField] float charge = 60f;
    [SerializeField] float maxCharge = 60f;
    [SerializeField] float criticalChargeFraction = 0.2f;

    [Header("Speeds")]

    [SerializeField] float dischargeSpeed = 1f;
    [SerializeField] float rechargeSpeed = 1f;

    Color color;

    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = maxCharge;
        slider.value = maxCharge;

        charge = maxCharge;
    }

    void Update()
    {
        if (Input.GetButtonDown("Flashlight"))
            ToggleLight();

        if (lightComponent.enabled)
        {
            charge -= dischargeSpeed * Time.deltaTime;

            if (charge <= 0)
                ToggleLight();
        }
        else
        {
            if (charge < maxCharge)
                charge += rechargeSpeed * Time.deltaTime;
            else if (charge > maxCharge)
                charge = maxCharge;
        }

        slider.value = charge;

        if (charge / maxCharge < criticalChargeFraction)
            color = new Color(1f, 0f, 0f, 0.5f);
        else
            color = new Color(1f, 1f, 1f, 0.5f);

        chargeBarIcon.color = color;
        chargeBarFill.color = color;

        chargeBar.SetActive(charge < maxCharge);
    }

    void ToggleLight()
    {
        audioController.PlayFlashlightSound();

        lightComponent.enabled = !lightComponent.enabled;
    }

    public void TurnOff()
    {
        audioController.PlayFlashlightSound();

        lightComponent.enabled = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField] GameObject staminaBar;
    [SerializeField] Image staminaBarIcon;
    [SerializeField] Image staminaBarFill;
    [SerializeField] Slider slider;
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] PlayerAudioController audioController;

    [Header("Values")]

    public float stamina = 30f;
    [SerializeField] float maxStamina = 30f;
    [SerializeField] float criticalStaminaFraction = 0.2f;

    public bool IsCriticalStamina => stamina / maxStamina <= criticalStaminaFraction;

    [Header("Speeds")]

    [SerializeField] float regenerationSpeed = 1f;
    [SerializeField] float runStaminaLossSpeed = 1f;

    float dyspneaVolume;

    Color color;

    void Start()
    {
        slider.minValue = 0f;
        slider.maxValue = maxStamina;
        slider.value = maxStamina;

        stamina = maxStamina;
    }

    void Update()
    {
        float change = playerMovement.State == PlayerMovement.MovementState.Running ? -runStaminaLossSpeed : regenerationSpeed;
        stamina = Mathf.Clamp(stamina + change * Time.deltaTime, 0f, maxStamina);

        slider.value = stamina;

        if (IsCriticalStamina)
            color = new Color(1f, 0f, 0f, 0.5f);
        else
            color = new Color(1f, 1f, 1f, 0.5f);

        staminaBarIcon.color = color;
        staminaBarFill.color = color;

        staminaBar.SetActive(stamina < maxStamina);

        // 1-stamina/maxStamina = [0; 1] * (1 - (1-critFraction)) + (1-critFraction)
        // [0; 1] = (1-stamina/maxStamina - (1-critFraction)) / (1 - (1-critFraction))
        dyspneaVolume = (criticalStaminaFraction - (stamina / maxStamina)) / criticalStaminaFraction;
        audioController.SetDyspneaSoundVolume(dyspneaVolume);
    }
}

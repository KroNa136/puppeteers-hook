using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class PostProcessingController : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Volume currentVolume;
    [SerializeField] VolumeProfile realisticProfile;
    [SerializeField] VolumeProfile retroProfile;
    [SerializeField] VolumeProfile oldProfile;

    [Header("Values")]

    [SerializeField] Filterer.FiltererMode currentFiltererMode = Filterer.FiltererMode.Realistic;
    [SerializeField] [Range(0f, 1f)] float normalVignetteIntensity = 0f;
    [SerializeField] [Range(0f, 1f)] float crouchingVignetteIntensity = 0.5f;

    [Header("Speeds")]

    [SerializeField] float valueChangeSpeed = 3f;

    Vignette vignette;

    Filterer.FiltererMode previousFiltererMode;

    void Start()
    {
        switch (currentFiltererMode)
        {
            case Filterer.FiltererMode.Realistic:
                currentVolume.profile = realisticProfile;
                break;
            case Filterer.FiltererMode.Retro:
                currentVolume.profile = retroProfile;
                break;
            case Filterer.FiltererMode.Old:
                currentVolume.profile = oldProfile;
                break;
        }
    }

    void Update()
    {
        if (currentFiltererMode != previousFiltererMode)
        {
            switch (currentFiltererMode)
            {
                case Filterer.FiltererMode.Realistic:
                    currentVolume.profile = realisticProfile;
                    break;
                case Filterer.FiltererMode.Retro:
                    currentVolume.profile = retroProfile;
                    break;
                case Filterer.FiltererMode.Old:
                    currentVolume.profile = oldProfile;
                    break;
            }

            previousFiltererMode = currentFiltererMode;
        }
    }

    public void AddCrouchingEffects()
    {
        currentVolume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, crouchingVignetteIntensity, valueChangeSpeed * Time.deltaTime);
    }

    public void RemoveCrouchingEffects()
    {
        currentVolume.profile.TryGet<Vignette>(out vignette);
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, normalVignetteIntensity, valueChangeSpeed * Time.deltaTime);
    }

    public void SetProfile(Filterer.FiltererMode filtererMode)
    {
        currentFiltererMode = filtererMode;
    }
}

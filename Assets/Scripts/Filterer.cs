using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;

public class Filterer : MonoBehaviour
{
    public enum FiltererMode
    {
        Realistic,
        Retro,
        Old
    }

    [Header("References")]

    [SerializeField] HDRenderPipelineAsset hdrpAsset;
    [SerializeField] FPSLimiter fpsLimiter;
    [SerializeField] Camera playerCamera;
    [SerializeField] RawImage cameraView;
    [SerializeField] Image colorFilter;
    [SerializeField] PostProcessingController postProcessing;

    [Header("Values")]

    [SerializeField] FiltererMode currentMode = FiltererMode.Realistic;

    [Header("Realistic Mode")]

    [SerializeField] int maxFPS_realistic = 60;

    [Header("Retro Mode")]

    [SerializeField] RenderTexture renderTexture_retro;
    [SerializeField] int maxFPS_retro = 50;
    [SerializeField] float resolutionScale_retro = 0.6f;

    [Header("Old Mode")]

    [SerializeField] RenderTexture renderTexture_old;
    [SerializeField] int maxFPS_old = 24;
    [SerializeField] float resolutionScale_old = 0.4f;

    FiltererMode previousMode;

    void Start()
    {
        switch (currentMode)
        {
            case FiltererMode.Realistic:
                //fpsLimiter.limitFPS = false;
                fpsLimiter.maxFPS = maxFPS_realistic;
                playerCamera.targetTexture = null;
                cameraView.texture = null;
                cameraView.gameObject.SetActive(false);
                colorFilter.gameObject.SetActive(false);
                break;
            case FiltererMode.Retro:
                //fpsLimiter.limitFPS = true;
                fpsLimiter.maxFPS = maxFPS_retro;
                playerCamera.targetTexture = renderTexture_retro;
                cameraView.texture = renderTexture_retro;
                cameraView.gameObject.SetActive(true);
                colorFilter.gameObject.SetActive(true);
                break;
            case FiltererMode.Old:
                //fpsLimiter.limitFPS = true;
                fpsLimiter.maxFPS = maxFPS_old;
                playerCamera.targetTexture = renderTexture_old;
                cameraView.texture = renderTexture_old;
                cameraView.gameObject.SetActive(true);
                colorFilter.gameObject.SetActive(true);
                break;
        }

        postProcessing.SetProfile(currentMode);

        previousMode = currentMode;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            switch (currentMode)
            {
                case FiltererMode.Realistic:
                    currentMode = FiltererMode.Retro;
                    break;
                case FiltererMode.Retro:
                    currentMode = FiltererMode.Old;
                    break;
                case FiltererMode.Old:
                    currentMode = FiltererMode.Realistic;
                    break;
            }
        }

        if (currentMode != previousMode)
        {
            var hdrpSettings = hdrpAsset.currentPlatformRenderPipelineSettings;

            switch (currentMode)
            {
                case FiltererMode.Realistic:
                    //fpsLimiter.limitFPS = false;
                    fpsLimiter.maxFPS = maxFPS_realistic;
                    /*
                    hdrpSettings.dynamicResolutionSettings.enabled = false;
                    renderTexture.width = Screen.width;
                    renderTexture.height = Screen.height;
                    */
                    playerCamera.targetTexture = null;
                    cameraView.texture = null;
                    cameraView.gameObject.SetActive(false);
                    colorFilter.gameObject.SetActive(false);
                    break;
                case FiltererMode.Retro:
                    //fpsLimiter.limitFPS = true;
                    fpsLimiter.maxFPS = maxFPS_retro;
                    /*
                    playerCamera.targetTexture = null;
                    hdrpSettings.dynamicResolutionSettings.enabled = true;
                    hdrpSettings.dynamicResolutionSettings.forceResolution = true;
                    hdrpSettings.dynamicResolutionSettings.forcedPercentage = 0.6f;
                    renderTexture.width = (int) Mathf.Floor(Screen.width * resolutionScale_retro);
                    renderTexture.height = (int) Mathf.Floor(Screen.height * resolutionScale_retro);
                    */
                    playerCamera.targetTexture = renderTexture_retro;
                    cameraView.texture = renderTexture_retro;
                    cameraView.gameObject.SetActive(true);
                    colorFilter.gameObject.SetActive(true);
                    break;
                case FiltererMode.Old:
                    //fpsLimiter.limitFPS = true;
                    fpsLimiter.maxFPS = maxFPS_old;
                    /*
                    playerCamera.targetTexture = null;
                    hdrpSettings.dynamicResolutionSettings.enabled = true;
                    hdrpSettings.dynamicResolutionSettings.forceResolution = true;
                    hdrpSettings.dynamicResolutionSettings.forcedPercentage = 0.6f;
                    renderTexture.width = (int) Mathf.Floor(Screen.width * resolutionScale_old);
                    renderTexture.height = (int) Mathf.Floor(Screen.height * resolutionScale_old);
                    */
                    playerCamera.targetTexture = renderTexture_old;
                    cameraView.texture = renderTexture_old;
                    cameraView.gameObject.SetActive(true);
                    colorFilter.gameObject.SetActive(true);
                    break;
            }

            postProcessing.SetProfile(currentMode);

            previousMode = currentMode;
        }
    }
}

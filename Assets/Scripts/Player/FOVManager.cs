using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOVManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Camera cameraComponent;

    [Header("Values")]

    [SerializeField] [Range(30f, 120f)] float normalFOV = 90f;
    [SerializeField] [Range(30f, 120f)] float runningFOV = 105f;
    [SerializeField] [Range(30f, 120f)] float crouchingFOV = 85f;
    [SerializeField] [Range(30f, 120f)] float fallingFOV = 60f;

    [Header("Speeds")]

    [SerializeField] float FOVAdjustmentSpeed = 2f;
    [SerializeField] float fallingFOVAdjustmentSpeed = 0.25f;

    void Start()
    {
        cameraComponent.fieldOfView = GetVerticalFOV(normalFOV);
    }

    public void SetNormalFOV()
    {
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, GetVerticalFOV(normalFOV), FOVAdjustmentSpeed * Time.deltaTime);
    }

    public void SetRunningFOV()
    {
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, GetVerticalFOV(runningFOV), FOVAdjustmentSpeed * Time.deltaTime);
    }
    
    public void SetCrouchingFOV()
    {
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, GetVerticalFOV(crouchingFOV), FOVAdjustmentSpeed * Time.deltaTime);
    }

    public void SetFallingFOV()
    {
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, GetVerticalFOV(fallingFOV), fallingFOVAdjustmentSpeed * Time.deltaTime);
    }

     float GetVerticalFOV(float horizontalFOV)
    {
        return Mathf.Rad2Deg * 2f * Mathf.Atan(Mathf.Tan((horizontalFOV * Mathf.Deg2Rad) / 2f) / cameraComponent.aspect);
    }
}

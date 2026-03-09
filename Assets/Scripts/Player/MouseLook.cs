using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Transform playerBody;
    [SerializeField] Transform cameraRoot;

    [Header("Control Abilities")]

    public bool enableHorizontalLook = true;
    public bool enableVerticalLook = true;

    [Header("Values")]

    [SerializeField] bool invertYAxis = false;
    [SerializeField] [Range(0.1f, 10f)] float sensitivity = 1f;
    public bool horizontalLookRotatesCamera = false;

    float mouseX;
    float mouseY;
    float xRotation;
    float yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (enableHorizontalLook)
            mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
        else
            mouseY = 0f;

        if (enableVerticalLook)
            mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;
        else
            mouseY = 0f;

        if (invertYAxis)
            xRotation += mouseY;
        else
            xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (horizontalLookRotatesCamera)
        {
            yRotation += mouseX;
        }
        else
        {
            yRotation = 0f;
            playerBody.Rotate(Vector3.up * mouseX);
        }

        cameraRoot.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}

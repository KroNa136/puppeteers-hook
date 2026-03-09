using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WardrobeDoor : MonoBehaviour
{
    [Header("References")]

    [SerializeField] WardrobeDoorAudioController audioController;

    [Header("Values")]

    [SerializeField] float openAngle = 120f;
    [SerializeField] float startSmoothMovementFraction = 0.333f;

    [Header("Speeds")]

    [SerializeField] float openLinearSpeed = 120f;
    [SerializeField] float openSmoothSpeed = 3.6f;
    [SerializeField] float closeLinearSpeed = 160f;
    [SerializeField] float closeSmoothSpeed = 4.8f;

    float defaultAngle;
    float targetAngle;

    float closedEulerAnglesY;
    float openedEulerAnglesY;

    bool isOpened = false;
    bool isOpening = false;
    bool isClosing = false;

    void Start()
    {
        defaultAngle = transform.eulerAngles.y;
        targetAngle = defaultAngle + openAngle;
        
        closedEulerAnglesY = defaultAngle;
        openedEulerAnglesY = closedEulerAnglesY + openAngle;

        if (openedEulerAnglesY > 360)
            openedEulerAnglesY -= 360;
        else if (openedEulerAnglesY < 0)
            openedEulerAnglesY += 360;
    }

    void Update()
    {
        if (isOpening)
        {
            if (Quaternion.Angle(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f)) / Quaternion.Angle(Quaternion.Euler(0f, defaultAngle, 0f), Quaternion.Euler(0f, targetAngle, 0f)) <= startSmoothMovementFraction)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), openSmoothSpeed * Time.deltaTime);
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), openLinearSpeed * Time.deltaTime);

            if (transform.eulerAngles.y > openedEulerAnglesY - 0.25f && transform.eulerAngles.y < openedEulerAnglesY + 0.25f)
            {
                transform.eulerAngles = new Vector3(0f, openedEulerAnglesY, 0f);
                isOpening = false;
                isOpened = true;
            }
        }
        else if (isClosing)
        {
            if (Quaternion.Angle(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f)) / Quaternion.Angle(Quaternion.Euler(0f, defaultAngle, 0f), Quaternion.Euler(0f, targetAngle, 0f)) >= 1f - startSmoothMovementFraction)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, defaultAngle, 0f), closeSmoothSpeed * Time.deltaTime);
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, defaultAngle, 0f), closeLinearSpeed * Time.deltaTime);

            if (transform.eulerAngles.y > closedEulerAnglesY - 0.25f && transform.eulerAngles.y < closedEulerAnglesY + 0.25f)
            {
                transform.eulerAngles = new Vector3(0f, closedEulerAnglesY, 0f);
                isClosing = false;

                audioController.enableHighSanityLossSounds = true;
            }
        }
    }

    public void Toggle()
    {
        if (isOpened || isOpening)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    void Open()
    {
        isClosing = false;
        isOpening = true;

        audioController.enableHighSanityLossSounds = false;
        audioController.PlayOpeningSound();
    }

    void Close()
    {
        isOpened = false;
        isOpening = false;
        isClosing = true;

        audioController.enableHighSanityLossSounds = false;
        audioController.PlayClosingSound();
    }
}

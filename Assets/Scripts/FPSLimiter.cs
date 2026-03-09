using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    public bool limitFPS = false;
    [Range(1, 200)] public int maxFPS = 60;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            limitFPS = !limitFPS;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            maxFPS = 16;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            maxFPS = 24;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            maxFPS = 30;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            maxFPS = 40;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            maxFPS = 48;
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            maxFPS = 50;
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            maxFPS = 60;

        if (limitFPS)
            Application.targetFrameRate = maxFPS;
        else
            Application.targetFrameRate = -1;
    }
}

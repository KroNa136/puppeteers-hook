using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Text fpsText;

    [Header("Values")]

    [SerializeField] float updateFrequency;

    float t = 0f;

    int updatesPassed = 0;

    void Update()
    {
        t += Time.deltaTime;
        updatesPassed++;

        if (t >= updateFrequency)
        {
            fpsText.text = Mathf.Floor(updatesPassed / t).ToString() + " FPS";

            t = 0f;
            updatesPassed = 0;
        }
    }
}

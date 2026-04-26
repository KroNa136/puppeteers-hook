using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadBobController : MonoBehaviour
{
    [Header("References")]
    
    [SerializeField] Transform mainCamera;
    [SerializeField] PlayerAudioController audioController;

    [Header("Options")]

    public bool enableHeadBob = true;
    public bool enableFootsteps = true;

    [Header("Amplitudes")]

    [SerializeField] float walkBobAmplitude = 0.004f;
    [SerializeField] float runBobAmplitude = 0.009f;

    [Header("Frequencies")]

    [SerializeField] float walkBobFrequency = 10f;
    [SerializeField] float runBobFrequency = 17f;

    [Header("Speeds")]

    [SerializeField] float resetSpeed = 5f;

    Vector3 startPos;

    Vector3 positionShift;
    float previousYShift;

    float t;

    bool playedFootstepSound = false;

    void Start()
    {
        startPos = mainCamera.localPosition;
    }

    void Update()
    {
        //Debug.Log(playedFootstepSound);
    }

    public void Walk()
    {
        if (!enableHeadBob)
            return;

        t += Time.deltaTime * walkBobFrequency;

        positionShift = new Vector3(Mathf.Sin(t / 2) * walkBobAmplitude / 2, Mathf.Sin(t) * walkBobAmplitude, 0f);

        if (enableFootsteps && positionShift.y > previousYShift)
        {
            if (!playedFootstepSound)
            {
                audioController.PlayFootstepSound();
                playedFootstepSound = true;
            }
        }
        else
        {
            playedFootstepSound = false;
        }

        mainCamera.localPosition += positionShift * Time.deltaTime;

        previousYShift = positionShift.y;
    }

    public void Run()
    {
        if (!enableHeadBob)
            return;

        t += Time.deltaTime * runBobFrequency;

        positionShift = new Vector3(Mathf.Sin(t / 2) * runBobAmplitude / 2, Mathf.Sin(t) * runBobAmplitude, 0f);

        if (enableFootsteps && positionShift.y > previousYShift)
        {
            if (!playedFootstepSound)
            {
                audioController.PlayFootstepSound();
                playedFootstepSound = true;
            }
        }
        else
        {
            playedFootstepSound = false;
        }

        mainCamera.localPosition += positionShift * Time.deltaTime;

        previousYShift = positionShift.y;
    }

    public void Reset()
    {
        if (!enableHeadBob)
            return;
        
        positionShift = new Vector3(Mathf.Lerp(mainCamera.localPosition.x, startPos.x, resetSpeed * Time.deltaTime) - mainCamera.localPosition.x, Mathf.Lerp(mainCamera.localPosition.y, startPos.y, resetSpeed * Time.deltaTime) - mainCamera.localPosition.y, 0f);
        mainCamera.localPosition += positionShift;
    }
}

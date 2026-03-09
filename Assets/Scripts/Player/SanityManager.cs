using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SanityManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField] Image sanityIndicator;
    [SerializeField] PlayerAudioController playerAudioController;

    [Header("Values")]

    public bool enableRegeneration = true;
    public float sanity = 100f;
    [SerializeField] float maxSanity = 100f;
    [SerializeField] bool enableSanityLossEffects = true;

    [Header("Speeds")]

    [SerializeField] float regenerationSpeed = 10f; // 10 seconds for 0-100
    [SerializeField] float decreaseSpeed = 0.333f; // 300 seconds (5 minutes) for 100-0
    [SerializeField] float decreaseToTargetSpeed = 3.333f; // 30-ish seconds for 100-0

    [Header("Separation")]

    [SerializeField] float lowSanityLossFraction = 0.75f;
    [SerializeField] float mediumSanityLossFraction = 0.5f;
    [SerializeField] float highSanityLossFraction = 0.25f;
    [SerializeField] float maximumSanityLossFraction = 0.01f;

    bool isDecreasingToTarget = false;
    bool isDecreasing = false;

    float targetSanity;

    float indicatorValue;

    DoorAudioController[] doorAudioControllers;
    WardrobeDoorAudioController[] wardrobeDoorAudioControllers;
    WallText[] wallTexts;
    Hallucination[] hallucinations;

    int i;
    
    void Start()
    {
        sanity = maxSanity;

        GetDoorAudioControllers();
        GetWardrobeDoorAudioControllers();
        GetWallTexts();
        GetHallucinations();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            DecreaseTest();

        if (isDecreasingToTarget || isDecreasing)
        {
            if (isDecreasingToTarget)
            {
                sanity = Mathf.Lerp(sanity, targetSanity, decreaseToTargetSpeed * Time.deltaTime);

                if (sanity - 0.1f < targetSanity)
                {
                    sanity = targetSanity;
                    isDecreasingToTarget = false;
                }
            }
            
            if (isDecreasing)
            {
                sanity -= decreaseSpeed * Time.deltaTime;

                if (sanity < 0f)
                {
                    sanity = 0f;
                    StopDecreasing();
                }
            }
        }
        else if (enableRegeneration)
        {
            if (sanity < maxSanity)
                sanity += regenerationSpeed * Time.deltaTime;
            else if (sanity > maxSanity)
                sanity = maxSanity;
        }

        if (enableSanityLossEffects)
        {
            playerAudioController.SetLowSanityLossSounds(sanity / maxSanity <= lowSanityLossFraction);
            playerAudioController.SetMediumSanityLossSounds(sanity / maxSanity <= mediumSanityLossFraction);

            for (i = 0; i < doorAudioControllers.Length; i++)
                doorAudioControllers[i].SetHighSanityLossSounds(sanity / maxSanity <= highSanityLossFraction);
            
            for (i = 0; i < wardrobeDoorAudioControllers.Length; i++)
                wardrobeDoorAudioControllers[i].SetHighSanityLossSounds(sanity / maxSanity <= highSanityLossFraction);
            
            for (i = 0; i < wallTexts.Length; i++)
                wallTexts[i].SetVisible(sanity / maxSanity <= highSanityLossFraction);

            for (i = 0; i < hallucinations.Length; i++)
                hallucinations[i].SetBehaviour(sanity / maxSanity <= maximumSanityLossFraction);
        }

        indicatorValue = 1f - sanity / maxSanity;

        sanityIndicator.color = new Color32((byte)255, (byte)255, (byte)255, (byte)(indicatorValue * 255));
        playerAudioController.SetSanityDecreaseAreaVolume(indicatorValue);
    }

    public void StartDecreasing()
    {
        isDecreasing = true;
    }

    public void StopDecreasing()
    {
        isDecreasing = false;
    }

    public void DecreaseBy(float amount)
    {
        targetSanity = sanity - amount;

        if (targetSanity < 0f)
            targetSanity = 0f;

        isDecreasingToTarget = true;
    }

    [ContextMenu("Decrease by 10")]
    void DecreaseTest()
    {
        DecreaseBy(10f);
    }

    public void GetDoorAudioControllers()
    {
        doorAudioControllers = FindObjectsByType<DoorAudioController>(FindObjectsSortMode.None);
    }

    public void GetWardrobeDoorAudioControllers()
    {
        wardrobeDoorAudioControllers = FindObjectsByType<WardrobeDoorAudioController>(FindObjectsSortMode.None);
    }

    public void GetWallTexts()
    {
        wallTexts = FindObjectsByType<WallText>(FindObjectsSortMode.None);
    }

    public void GetHallucinations()
    {
        hallucinations = FindObjectsByType<Hallucination>(FindObjectsSortMode.None);
    }
}

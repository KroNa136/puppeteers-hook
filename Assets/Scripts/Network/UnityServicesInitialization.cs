using System;
using Unity.Services.Core;
using UnityEngine;

public class UnityServicesInitialization : MonoBehaviour
{
    async void Awake()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}

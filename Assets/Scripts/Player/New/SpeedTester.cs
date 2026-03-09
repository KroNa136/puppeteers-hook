using System.Collections;
using UnityEngine;

public class SpeedTester : MonoBehaviour
{
    [SerializeField] private Transform _ghost;
    [SerializeField] private float _ghostDashSpeed = 6f;
    [SerializeField] private float _ghostWalkingSpeed = 1f;
    [SerializeField] private float _ghostDashTime = 1f;
    [SerializeField] private float _ghostDashRecoveryTime = 3f;

    [SerializeField] private Transform _investigator;
    [SerializeField] private float _investigatorRunningSpeed = 3f;
    [SerializeField] private float _investigatorWalkingSpeed = 1.5f;
    [SerializeField] private float _investigatorRunningTime = 20f;
    [SerializeField] private float _investigatorRunningRecoveryTime = 20f;

    private float _ghostCurrentSpeed = 0f;
    private float _investigatorCurrentSpeed = 0f;

    private void Start()
    {
        _ = StartCoroutine(GhostLoop());
        _ = StartCoroutine(InvestigatorLoop());
    }

    private void Update()
    {
        _ghost.Translate(_ghostCurrentSpeed * Time.deltaTime * _ghost.forward);
        _investigator.Translate(_investigatorCurrentSpeed * Time.deltaTime * _investigator.forward);
    }

    private IEnumerator GhostLoop()
    {
        while (true)
        {
            _ghostCurrentSpeed = _ghostDashSpeed;
            yield return new WaitForSeconds(_ghostDashTime);
            _ghostCurrentSpeed = _ghostWalkingSpeed;
            yield return new WaitForSeconds(_ghostDashRecoveryTime);
        }
    }

    private IEnumerator InvestigatorLoop()
    {
        while (true)
        {
            _investigatorCurrentSpeed = _investigatorRunningSpeed;
            yield return new WaitForSeconds(_investigatorRunningTime);
            _investigatorCurrentSpeed = _investigatorWalkingSpeed;
            yield return new WaitForSeconds(_investigatorRunningRecoveryTime);
        }
    }
}

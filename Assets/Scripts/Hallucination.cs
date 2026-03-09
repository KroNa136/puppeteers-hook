using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hallucination : MonoBehaviour
{
    enum HallucinationBehaviour
    {
        Vanish,
        MoveToPosition
    }

    [Header("References")]

    [SerializeField] GameObject obj;
    [SerializeField] Collider trigger;
    [SerializeField] Camera playerCameraComponent;
    [SerializeField] Transform playerCamera;

    [Header("Options")]
    
    public bool enableBehaviour = false;
    [SerializeField] HallucinationBehaviour behaviourType = HallucinationBehaviour.Vanish;
    [SerializeField] bool useMaxDistanceToDisappear = false;

    [Header("Values")]

    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float timeToDisappear = 0.22f; // human max reaction time for visuals
    [SerializeField] float minTimeToReappear = 60f; // 1 minute
    [SerializeField] float maxTimeToReappear = 300f; // 5 minutes
    [SerializeField] float maxDistanceToDisappear = 10f;
    [SerializeField] Vector3 localPositionToMove = new Vector3(0f, 0f, 0f);
    [SerializeField] float moveSpeed = 5f;

    Vector3 originalLocalPosition;

    float t = 0;
    float timeToReappear;

    void Start()
    {
        originalLocalPosition = obj.transform.localPosition;
    }

    void Update()
    {
        if (enableBehaviour)
        {
            if (obj.activeInHierarchy)
            {
                if (IsVisible())
                {
                    t += Time.deltaTime;

                    if (t >= timeToDisappear)
                    {
                        if (!useMaxDistanceToDisappear || (useMaxDistanceToDisappear && Vector3.Distance(obj.transform.position, playerCamera.position) <= maxDistanceToDisappear))
                        {
                            t = 0;
                            timeToReappear = Random.Range(minTimeToReappear, maxTimeToReappear);

                            if (behaviourType == HallucinationBehaviour.Vanish)
                            {
                                obj.SetActive(false);
                                obj.transform.localPosition = originalLocalPosition;
                            }
                            else
                            {
                                StartCoroutine(MoveAway());
                            }
                        }
                    }
                }
            }
            else
            {
                t += Time.deltaTime;

                if (t >= timeToReappear && !IsVisible())
                {
                    if (!useMaxDistanceToDisappear || (useMaxDistanceToDisappear && Vector3.Distance(obj.transform.position, playerCamera.position) > maxDistanceToDisappear))
                    {
                        t = 0;

                        obj.SetActive(true);
                        obj.transform.localPosition = originalLocalPosition;
                    }
                }
            }
        }
        else
        {
            t = 0;
            obj.SetActive(false);
        }
    }

    public void SetBehaviour(bool value)
    {
        enableBehaviour = value;
    }
    
    bool IsVisible()
    {
        return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(playerCameraComponent), trigger.bounds)
            && !Physics.Linecast(trigger.transform.position, playerCamera.position, obstacleMask);
    }

    IEnumerator MoveAway()
    {
        while (obj.transform.localPosition != localPositionToMove)
        {
            obj.transform.localPosition = Vector3.Slerp(obj.transform.localPosition, localPositionToMove, moveSpeed * Time.deltaTime);

            if (obj.transform.localPosition.x > localPositionToMove.x - 0.1f && obj.transform.localPosition.x < localPositionToMove.x + 0.1f
                && obj.transform.localPosition.y > localPositionToMove.y - 0.1f && obj.transform.localPosition.y < localPositionToMove.y + 0.1f
                && obj.transform.localPosition.z > localPositionToMove.z - 0.1f && obj.transform.localPosition.z < localPositionToMove.z + 0.1f)
                obj.transform.localPosition = localPositionToMove;
            
            yield return null;
        }

        obj.SetActive(false);

        yield return null;
    }
}

using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ReflectableLightSource : NetworkBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LayerMask _surfacesLayerMask;
    [SerializeField][Min(0f)] private float _nextRaycastOffset = 0.01f;
    [SerializeField][Min(1)] private int _maxReflections = 10;

    private RaycastHit _hit;
    private readonly List<ReflectableLightTarget> _hitTargets = new();

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        List<Vector3> linePoints = new() { transform.position };

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = transform.forward;

        List<ReflectableLightTarget> hitTargets = new();

        int reflections = 0;

        while (reflections <= _maxReflections)
        {
            if (!Physics.Raycast(rayOrigin, rayDirection, out _hit, float.PositiveInfinity, _surfacesLayerMask, QueryTriggerInteraction.Ignore))
                break;

            linePoints.Add(_hit.point);

            if (_hit.collider == null)
                break;

            if (_hit.collider.TryGetComponent(out ReflectableLightTarget target))
            {
                target.IsHit = true;
                hitTargets.Add(target);
            }

            if (!_hit.collider.CompareTag("ReflectiveSurface"))
                break;

            reflections++;

            rayOrigin = _hit.point - rayDirection * _nextRaycastOffset;
            rayDirection = Vector3.Reflect(rayDirection, _hit.normal);
        }

        var previuoslyHitButNotCurrentlyHitTargets = _hitTargets.Except(hitTargets);

        foreach (var target in previuoslyHitButNotCurrentlyHitTargets)
            target.IsHit = false;

        _hitTargets.Clear();
        _hitTargets.AddRange(hitTargets);

        Vector3[] linePointsArray = linePoints.ToArray();

        _lineRenderer.positionCount = linePointsArray.Length;
        _lineRenderer.SetPositions(linePointsArray);

        RpcDisplayLine(linePointsArray);
    }

    [ClientRpc]
    public void RpcDisplayLine(Vector3[] linePoints)
    {
        _lineRenderer.positionCount = linePoints.Length;
        _lineRenderer.SetPositions(linePoints);
    }
}

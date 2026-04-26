using System.Linq;
using Mirror;
using UnityEngine;

public class InvestigatorWinTrigger : NetworkBehaviour
{
    [SerializeField] private BoxCollider _trigger;
    [SerializeField] private LayerMask _investigatorLayerMask;

    private Collider[] _overlaps;
    private bool _winTriggered;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        _overlaps = new Collider[20];
        _winTriggered = false;

        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (_winTriggered)
            return;

        int overlapCount = Physics.OverlapBoxNonAlloc
        (
            center: _trigger.transform.position + _trigger.center,
            halfExtents: _trigger.bounds.extents,
            results: _overlaps,
            orientation: _trigger.transform.rotation,
            mask: _investigatorLayerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        bool investigatorIsInside = _overlaps
            .Take(overlapCount)
            .Any(o => o.TryGetComponent(out InvestigatorPlayerMovement _));

        if (investigatorIsInside)
        {
            _winTriggered = true;
            GameManager.Instance.ServerInvestigatorWin();
        }
    }
}

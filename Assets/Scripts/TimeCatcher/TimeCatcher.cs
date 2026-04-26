using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class TimeCatcher : NetworkBehaviour
{
    //[SerializeField] private TimeCatcherAudioController _audioController;
    [SerializeField] private Collider _collider;

    [Space]

    [SerializeField] private ParticleSystem _onActivatedParticleSystem;
    [SerializeField] private Vector3 _touchedInvestigatorsCheckColliderExtentsInflation = new(0.1f, 0.1f, 0.1f);
    [SerializeField] private int _gameTimeDecrease = 10;
    [SerializeField] private float _destroyDelay = 5f;

    [SyncVar(hook = nameof(OnClientIsActivatedChanged))]
    public bool IsActivated;

    private Collider[] _playerOverlaps;
    private bool _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = false;

    [Client]
    public void OnClientIsActivatedChanged(bool oldValue, bool newValue)
    {
        if (!isClient)
            return;

        if (oldValue == newValue)
            return;

        if (!newValue)
            return;

        _ = _onActivatedParticleSystem
            .Bind(ps => { if (ps.isPlaying) ps.Stop(); })
            .Bind(ps => ps.Play());

        // TODO: dissolution animation
        // _ = _audioController.Bind(c => c.PlayActivationSound());
    }

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        IsActivated = false;

        _playerOverlaps = new Collider[20];

        if (ServerGetTouchedInvestigators().Any())
            _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = true;
        
        TickSystem.Instance.OnTick.AddListener(ServerTick);
    }

    [Server]
    public void ServerTick(int tick)
    {
        if (!isServer)
            return;

        if (IsActivated)
            return;

        if (ServerGetTouchedInvestigators().Any())
        {
            if (_isWaitingForInvestigatorsToLeaveTriggerAfterSpawn)
                return;

            IsActivated = true;
            GameManager.Instance.ServerDecreaseGameTimeBy(_gameTimeDecrease);
            Invoke(nameof(ServerDestroy), _destroyDelay);
        }
        else
        {
            _isWaitingForInvestigatorsToLeaveTriggerAfterSpawn = false;
        }
    }

    [Server]
    public void ServerDestroy()
    {
        if (!isServer)
            return;

        TickSystem.Instance.OnTick.RemoveListener(ServerTick);
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private IEnumerable<InvestigatorPlayerMovement> ServerGetTouchedInvestigators()
    {
        if (!isServer)
            return CreateList.With<InvestigatorPlayerMovement>();

        Vector3 inflatedExtents = _collider.bounds.extents + _touchedInvestigatorsCheckColliderExtentsInflation;

        int overlapCount = Physics.OverlapBoxNonAlloc
        (
            center: _collider.bounds.center,
            halfExtents: inflatedExtents,
            results: _playerOverlaps,
            orientation: transform.rotation
        );

        var touchedInvestigators = _playerOverlaps
            .Take(overlapCount)
            .Select(o => o.TryGetComponent(out InvestigatorPlayerMovement investigatorPlayerMovement) ? investigatorPlayerMovement : null)
            .NonNullItems()
            .ToList();

        return touchedInvestigators;
    }
}

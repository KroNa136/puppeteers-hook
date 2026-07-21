using Mirror;
using UnityEngine;

public class Holdable : NetworkBehaviour
{
    [SerializeField] private HoldableType _type;
    public HoldableType Type => _type;

    [SerializeField] private HoldableAudioController _audioController;
    [SerializeField] private Rigidbody _rigidbody;

    [Space]

    [SerializeField] private float _minHitVelocityForHitSound = 0.1f;

    private Transform _parent;

    public bool IsHeld => _parent != null;

    [Server]
    public override void OnStartServer()
    {
        if (!isServer)
            return;

        if (TryGetComponent(out Interactable interactable))
            interactable.OnInteract.AddListener(ServerToggle);
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        if (_parent != null)
            _rigidbody.Move(_parent.position, Quaternion.Euler(0f, _parent.eulerAngles.y, 0f));
    }

    [Server]
    public void ServerToggle(NetworkConnectionToClient conn)
    {
        if (!isServer)
            return;

        if (!conn.identity.TryGetComponent(out Hand hand))
            return;

        if (IsHeld)
        {
            _parent = null;
            _rigidbody.useGravity = true;
        }
        else
        {
            _parent = hand.HandObject;
            _rigidbody.useGravity = false;
        }
    }

    [Server]
    public void ServerDisableInteraction()
    {
        if (!isServer)
            return;

        DisableInteraction();
        RpcDisableInteraction();
    }

    [ClientRpc]
    public void RpcDisableInteraction()
    {
        if (isServer)
            return;

        DisableInteraction();
    }

    private void DisableInteraction()
    {
        if (TryGetComponent(out Interactable interactable))
            interactable.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude >= _minHitVelocityForHitSound)
            _ = _audioController.Bind(c => c.PlayHitSound());
    }
}

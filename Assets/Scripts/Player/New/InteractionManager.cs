using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : NetworkBehaviour
{
    [SerializeField] private Transform _camera;
    [SerializeField] private GameObject _uiCrosshair;
    [SerializeField] private PlayerAudioController _audioController;

    [Space]

    [SerializeField] private float _maxDistance = 1.5f;
    [SerializeField] private LayerMask _layerMask;

    private InputManager _inputManager;
    private RaycastHit _hit;
    private Interactable _currentTarget;

    private void Start()
    {
        if (!isLocalPlayer)
            return;

        _inputManager = GetComponent<InputManager>();
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        ClientCheckForTarget();
        ClientHandleInput();
    }

    [Client]
    public void ClientCheckForTarget()
    {
        if (!isLocalPlayer)
            return;

        if (Physics.Raycast(origin: _camera.position, direction: _camera.forward, out _hit, _maxDistance, _layerMask) &&
            _hit.collider.TryGetComponent(out Interactable interactable) &&
            interactable.IsAuthorizedToInteract(PlayerData.Local.Role))
        {
            _uiCrosshair.SetActive(true);
            _currentTarget = interactable;
        }
        else
        {
            _uiCrosshair.SetActive(false);
            _currentTarget = null;
        }
    }

    [Client]
    public void ClientHandleInput()
    {
        if (!isLocalPlayer)
            return;

        if (!_inputManager.Interact)
            return;

        CmdInteract(_currentTarget);
        _audioController.PlayInteractionSound(_currentTarget != null);
    }

    [Command]
    public void CmdInteract(Interactable target, NetworkConnectionToClient conn = null)
    {
        if (target == null)
            return;

        if (!Physics.Raycast(_camera.position, _camera.forward, out _hit, _maxDistance, _layerMask))
            return;

        if (!_hit.collider.TryGetComponent(out Interactable interactable))
            return;

        if (target != interactable)
            return;

        var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);

        if (playerData == null || !target.IsAuthorizedToInteract(playerData.Role))
            return;

        interactable.ServerInteract(conn);
    }
}

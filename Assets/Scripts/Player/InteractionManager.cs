using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : NetworkBehaviour
{
    [SerializeField] private PlayerAudioController _audioController;

    private GameHud _gameHud;
    private Transform _cameraRoot;

    [Space]

    [SerializeField] private float _maxDistance = 1.5f;
    [SerializeField] private LayerMask _layerMask;

    private InputManager _inputManager;
    private RaycastHit _hit;
    private Interactable _currentTarget;

    public bool CanBeControlledByPlayer = true;

    private void Start()
    {
        _cameraRoot = GetComponent<CameraController>().CameraRoot;

        GameManager.OnClientGameOver.AddListener(OnGameOver);
    }

    private void OnGameOver(bool _)
    {
        CanBeControlledByPlayer = false;
    }

    public override void OnStartAuthority()
    {
        if (!isLocalPlayer)
            return;

        _gameHud = FindAnyObjectByType<GameHud>();
        _ = _gameHud.Bind(hud => hud.SetCrosshair(false));

        if (!TryGetComponent(out _inputManager))
            _ = StartCoroutine(WaitForInputManager());
    }

    [Client]
    public IEnumerator WaitForInputManager()
    {
        while (!TryGetComponent(out _inputManager))
            yield return new WaitForSeconds(0.5f);
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (CanBeControlledByPlayer)
            ClientHandleInput();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        ClientCheckForTarget();
    }

    [Client]
    public void ClientCheckForTarget()
    {
        if (!isLocalPlayer)
            return;

        if (Physics.Raycast(origin: _cameraRoot.position, direction: _cameraRoot.forward, out _hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore) &&
            _hit.collider != null &&
            (_hit.collider.TryGetComponent(out Interactable interactable) || _hit.collider.TryGetComponentInParent(out interactable)) &&
            interactable.isActiveAndEnabled &&
            interactable.IsAuthorizedToInteract(PlayerData.Local.Role))
        {
            _ = _gameHud.Bind(hud => hud.SetCrosshair(true));
            _currentTarget = interactable;
        }
        else
        {
            _ = _gameHud.Bind(hud => hud.SetCrosshair(false));
            _currentTarget = null;
        }
    }

    [Client]
    public void ClientHandleInput()
    {
        if (!isLocalPlayer)
            return;

        if (!CanBeControlledByPlayer || !_inputManager.Interact)
            return;

        _ = _currentTarget.Bind(t => t.ClientPredictInteraction());
        CmdInteract(_currentTarget);

        _ = _audioController.Bind((controller, success) => controller.PlayInteractionSound(success), _currentTarget != null);
    }

    [Command]
    public void CmdInteract(Interactable target, NetworkConnectionToClient conn = null)
    {
        if (target == null)
            return;

        if (!Physics.Raycast(origin: _cameraRoot.position, direction: _cameraRoot.forward, out _hit, _maxDistance, _layerMask, QueryTriggerInteraction.Ignore) ||
            _hit.collider == null ||
            !(_hit.collider.TryGetComponent(out Interactable interactable) || _hit.collider.TryGetComponentInParent(out interactable)) ||
            !interactable.isActiveAndEnabled ||
            target != interactable)
        {
            target.TargetRpcFailInteraction(conn);
            return;
        }

        var playerData = LobbyNetworkManager.Instance.GetConnectedPlayerData(conn);

        if (playerData == null || !target.IsAuthorizedToInteract(playerData.Role))
            return;

        interactable.ServerInteract(conn);
    }
}

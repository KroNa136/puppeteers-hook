using Mirror;
using UnityEngine;

public class GhostStealthAbility : GhostAbility
{
    [SerializeField] private GhostVisibilityManager _visibilityManager;

    protected override void PlayActivationSound()
    {
        _ = _audioController.Bind(c => c.PlayStealthAbilityActivationSound());
    }

    [Server]
    public override void ServerDoActivation()
    {
        if (!isServer)
            return;

        _ = _visibilityManager.Bind(vm => vm.ServerSetZeroVisibility());
    }

    [Server]
    public override void ServerDoDeactivation()
    {
        if (!isServer)
            return;

        _ = _visibilityManager.Bind(vm => vm.ServerSetLowVisibility());
    }
}

using Mirror;
using UnityEngine;

public class GhostTimeCatcherAbility : GhostAbility
{
    [SerializeField] private Vector3 _positionOffset = new(0f, 0f, 0.5f);

    protected override void PlayActivationSound()
    {
        _ = _audioController.Bind(c => c.PlayTimeCatcherAbilityActivationSound());
    }

    [Server]
    public override void ServerDoActivation()
    {
        if (!isServer)
            return;

        _ = _animator.Bind(a => a.SetTrigger("Summon"));

        Vector3 position = transform.position + transform.right * _positionOffset.x + transform.up * _positionOffset.y + transform.forward * _positionOffset.z;
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y + 180f, 0f);

        _ = WorldGenerator.Instance.ServerSpawnTimeCatcher(position, rotation);
    }

    [Server]
    public override void ServerDoDeactivation() { }
}

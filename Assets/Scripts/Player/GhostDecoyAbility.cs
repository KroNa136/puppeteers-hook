using Mirror;
using UnityEngine;

public class GhostDecoyAbility : GhostAbility
{
    [SerializeField] private int _minNoteLength = 100;
    [SerializeField] private int _maxNoteLength = 200;

    [Space]

    [SerializeField] private float _groundYOffset = 0.01f;

    [Server]
    public override void ServerDoActivation()
    {
        if (!isServer)
            return;

        int length = Random.Range(_minNoteLength, _maxNoteLength);

        Vector3 position = TryGetComponent(out GamePlayerMovement gamePlayerMovement)
            ? new Vector3(transform.position.x, gamePlayerMovement.BottomY + _groundYOffset, transform.position.z)
            : transform.position;

        var note = WorldGenerator.Instance.ServerSpawnNote(position, transform.rotation);
        note.ServerSetFullyCorruptedText(length);
    }

    [Server]
    public override void ServerDoDeactivation() { }
}

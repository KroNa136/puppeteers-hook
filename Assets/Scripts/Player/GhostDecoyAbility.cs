using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

public class GhostDecoyAbility : GhostAbility
{
    [SerializeField] private int _minNoteLength = 100;
    [SerializeField] private int _maxNoteLength = 200;

    [Space]

    [SerializeField] private float _groundYOffset = 0.01f;

    protected override void PlayActivationSound()
    {
        _ = _audioController.Bind(c => c.PlayDecoyAbilityActivationSound());
    }

    [Server]
    public override void ServerDoActivation()
    {
        if (!isServer)
            return;

        _ = _animator.Bind(a => a.SetTrigger("Summon"));

        int length = Random.Range(_minNoteLength, _maxNoteLength);

        Vector3 position = TryGetComponent(out GamePlayerMovement gamePlayerMovement)
            ? new Vector3(transform.position.x, gamePlayerMovement.BottomY + _groundYOffset, transform.position.z)
            : transform.position;

        _ = StartCoroutine(ServerSpawnNote(position, length));
    }

    [Server]
    public IEnumerator ServerSpawnNote(Vector3 position, int textLength)
    {
        if (!isServer)
            yield break;

        var note = WorldGenerator.Instance.ServerSpawnNote(position, transform.rotation);
        yield return new WaitForSeconds(0.3f);
        note.ServerSetFullyCorruptedText(textLength);
    }

    [Server]
    public override void ServerDoDeactivation() { }
}

using UnityEngine;

public class GhostAudioController : GamePlayerAudioController
{
    [SerializeField] private AudioSource _dashAudioSource;
    [SerializeField] private AudioSource _induceFearAudioSource;
    [SerializeField] private AudioSource _abilitiesAudioSource;

    [Space]

    [SerializeField] private AudioClip _prepareToDashAudioClip;
    [SerializeField] private AudioClip _dashAudioClip;
    [SerializeField] private AudioClip _induceFearAudioClip;
    [SerializeField] private AudioClip _decoyAbilityActivationAudioClip;
    [SerializeField] private AudioClip _doorLockAbilityActivationAudioClip;
    [SerializeField] private AudioClip _illusionAbilityActivationAudioClip;
    [SerializeField] private AudioClip _stealthAbilityActivationAudioClip;
    [SerializeField] private AudioClip _timeCatcherAbilityActivationAudioClip;
    [SerializeField] private AudioClip _abilityDeactivationAudioClip;
    [SerializeField] private AudioClip _abilityRestorationAudioClip;

    public void PlayPrepareToDashSound()
    {
        _ = _dashAudioSource.Bind(a => _prepareToDashAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayDashSound()
    {
        _ = _dashAudioSource.Bind(a => _dashAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayInduceFearSound()
    {
        _ = _induceFearAudioSource.Bind(a => _induceFearAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayDecoyAbilityActivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _decoyAbilityActivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayDoorLockAbilityActivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _doorLockAbilityActivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayIllusionAbilityActivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _illusionAbilityActivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayStealthAbilityActivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _stealthAbilityActivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayTimeCatcherAbilityActivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _timeCatcherAbilityActivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayAbilityDeactivationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _abilityDeactivationAudioClip.Bind(c => a.PlayOneShot(c)));
    }

    public void PlayAbilityRestorationSound()
    {
        _ = _abilitiesAudioSource.Bind(a => _abilityRestorationAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}

using UnityEngine;

public class UiAudioController : MonoBehaviour
{
    public static UiAudioController Instance;

    [SerializeField] private AudioSource _audioSource;

    [Space]

    [SerializeField] private AudioClip _pressButtonAudioClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        _ = Camera.main.Bind(c => transform.position = c.transform.position);
    }

    public void PlayPressButtonSound()
    {
        _ = _audioSource.Bind(a => _pressButtonAudioClip.Bind(c => a.PlayOneShot(c)));
    }
}

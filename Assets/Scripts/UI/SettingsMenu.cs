using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : Menu
{
    private const string WORLD_VOLUME_KEY = "world_volume";
    private const string FX_VOLUME_KEY = "fx_volume";
    private const string MUSIC_VOLUME_KEY = "music_volume";

    [SerializeField] private Slider _worldVolumeSlider;
    [SerializeField] private Slider _fxVolumeSlider;
    [SerializeField] private Slider _musicVolumeSlider;
    
    [SerializeField] private AudioMixer _audioMixer;

    protected override void OnStart()
    {
        bool firstLaunch = !PlayerPrefs.HasKey(WORLD_VOLUME_KEY);

        if (firstLaunch)
        {
            _worldVolumeSlider.value = 1f;
            _fxVolumeSlider.value = 1f;
            _musicVolumeSlider.value = 1f;

            _ = _audioMixer.SetFloat(WORLD_VOLUME_KEY, SliderValueToVolume(1f));
            _ = _audioMixer.SetFloat(FX_VOLUME_KEY, SliderValueToVolume(1f));
            _ = _audioMixer.SetFloat(MUSIC_VOLUME_KEY, SliderValueToVolume(1f));
        }
        else
        {
            _worldVolumeSlider.value = PlayerPrefs.GetFloat(WORLD_VOLUME_KEY);
            _fxVolumeSlider.value = PlayerPrefs.GetFloat(FX_VOLUME_KEY);
            _musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY);

            _ = _audioMixer.SetFloat(WORLD_VOLUME_KEY, SliderValueToVolume(_worldVolumeSlider.value));
            _ = _audioMixer.SetFloat(FX_VOLUME_KEY, SliderValueToVolume(_fxVolumeSlider.value));
            _ = _audioMixer.SetFloat(MUSIC_VOLUME_KEY, SliderValueToVolume(_musicVolumeSlider.value));
        }
    }

    public void SetWorldVolume()
    {
        PlayerPrefs.SetFloat(WORLD_VOLUME_KEY, _worldVolumeSlider.value);
        _ = _audioMixer.SetFloat(WORLD_VOLUME_KEY, SliderValueToVolume(_worldVolumeSlider.value));
    }

    public void SetFxVolume()
    {
        PlayerPrefs.SetFloat(FX_VOLUME_KEY, _fxVolumeSlider.value);
        _ = _audioMixer.SetFloat(FX_VOLUME_KEY, SliderValueToVolume(_fxVolumeSlider.value));
    }

    public void SetMusicVolume()
    {
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, _musicVolumeSlider.value);
        _ = _audioMixer.SetFloat(MUSIC_VOLUME_KEY, SliderValueToVolume(_musicVolumeSlider.value));
    }

    private float SliderValueToVolume(float value)
    {
        // These values are in dB.
        float maxVolume = 0f;
        float minVolume = -80f;

        // Converting from a linear scale to a logarithmic scale in such a way
        // that the change in volume sounds subjectively close to linear.
        // https://stackoverflow.com/questions/46529147/how-to-set-a-mixers-volume-to-a-sliders-volume-in-unity
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * (maxVolume - minVolume) / 4f + maxVolume;
    }

    protected override void OnDeactivate()
        => PlayerPrefs.Save();
}

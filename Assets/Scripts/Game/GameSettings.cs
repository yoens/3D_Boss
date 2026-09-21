
using UnityEngine;

public static class GameSettings
{
    private const string VolumeKey = "MasterVolume";
    private const string SensitivityKey = "MouseSensitivity";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);

        set
        {
            PlayerPrefs.SetFloat(
                VolumeKey,
                Mathf.Clamp01(value)
            );

            AudioListener.volume = Mathf.Clamp01(value);
        }
    }

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(SensitivityKey, 1f);

        set
        {
            PlayerPrefs.SetFloat(
                SensitivityKey,
                Mathf.Clamp(value, 0.1f, 5f)
            );
        }
    }

    public static void ApplySettings()
    {
        AudioListener.volume = MasterVolume;
    }
}
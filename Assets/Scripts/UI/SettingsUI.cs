
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;

    private void OnEnable()
    {
        masterVolumeSlider.SetValueWithoutNotify(
            GameSettings.MasterVolume
        );

        mouseSensitivitySlider.SetValueWithoutNotify(
            GameSettings.MouseSensitivity
        );

        masterVolumeSlider.onValueChanged.AddListener(
            ChangeVolume
        );

        mouseSensitivitySlider.onValueChanged.AddListener(
            ChangeSensitivity
        );
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(
            ChangeVolume
        );

        mouseSensitivitySlider.onValueChanged.RemoveListener(
            ChangeSensitivity
        );

        PlayerPrefs.Save();
    }

    private void ChangeVolume(float value)
    {
        GameSettings.MasterVolume = value;
    }

    private void ChangeSensitivity(float value)
    {
        GameSettings.MouseSensitivity = value;
    }
}
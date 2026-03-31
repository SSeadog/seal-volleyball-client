using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeSettingsPopup : MonoBehaviour
{
    [SerializeField] private Button btnClose;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text bgmValueText;
    [SerializeField] private TMP_Text sfxValueText;

    private void OnEnable()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(Hide);
            btnClose.onClick.AddListener(Hide);
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.wholeNumbers = false;
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (SoundManager.Instance != null)
        {
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(SoundManager.Instance.GetBgmVolume());
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetSfxVolume());
        }

        RefreshValueTexts();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnBgmChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBgmVolume(value, save: true);
        RefreshValueTexts();
    }

    private void OnSfxChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSfxVolume(value, save: true);
        RefreshValueTexts();
    }

    private void RefreshValueTexts()
    {
        if (bgmValueText != null && bgmSlider != null)
            bgmValueText.text = $"{Mathf.RoundToInt(bgmSlider.value * 100f)}%";
        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
    }
}


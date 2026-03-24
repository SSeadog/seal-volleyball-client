using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이 방법 팝업. 슬라이드는 Inspector에서 배열 순서대로 등록합니다.
/// </summary>
public class PopupHowToPlay : MonoBehaviour
{
    [Header("Slides (순서대로 표시)")]
    [SerializeField] private GameObject[] slides;

    [Header("Buttons")]
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnPrev;
    [SerializeField] private Button btnNext;

    private int _currentIndex;

    private void Awake()
    {
        if (btnClose != null)
        {
            btnClose.onClick.AddListener(OnClickClose);
        }

        if (btnPrev != null)
        {
            btnPrev.onClick.AddListener(OnClickPrev);
        }

        if (btnNext != null)
        {
            btnNext.onClick.AddListener(OnClickNext);
        }
    }

    private void OnEnable()
    {
        RefreshSlides();
    }

    /// <summary> 팝업을 열고 첫 슬라이드부터 표시합니다. </summary>
    public void ShowPopup()
    {
        _currentIndex = 0;
        gameObject.SetActive(true);
        RefreshSlides();
    }

    /// <summary> 팝업을 닫습니다. </summary>
    public void HidePopup()
    {
        gameObject.SetActive(false);
    }

    private void OnClickClose()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx("Btn_Click");
        HidePopup();
    }

    private void OnClickPrev()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx("Btn_Click");

        if (slides == null || slides.Length == 0) return;
        if (_currentIndex <= 0) return;

        _currentIndex--;
        RefreshSlides();
    }

    private void OnClickNext()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx("Btn_Click");

        if (slides == null || slides.Length == 0) return;
        if (_currentIndex >= slides.Length - 1) return;

        _currentIndex++;
        RefreshSlides();
    }

    private void RefreshSlides()
    {
        if (slides == null || slides.Length == 0)
        {
            if (btnPrev != null) btnPrev.interactable = false;
            if (btnNext != null) btnNext.interactable = false;
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
                slides[i].SetActive(i == _currentIndex);
        }

        // 첫 장 / 마지막 장에서 좌우 버튼 비활성화
        if (btnPrev != null)
            btnPrev.interactable = _currentIndex > 0;
        if (btnNext != null)
            btnNext.interactable = _currentIndex < slides.Length - 1;
    }
}

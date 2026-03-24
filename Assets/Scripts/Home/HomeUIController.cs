using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TMP_InputField roomCodeInput; 
    [SerializeField] private Button btnJoinLobby;
    [SerializeField] private Button btnCreateLobby;
    [SerializeField] private Button btnHowToPlay;
    [SerializeField] private PopupHowToPlay popupHowToPlay;
    
    void Start()
    {
        InitializeNickname();

        btnCreateLobby.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            // roomId 초기화 (새로 만들 때)
            LobbyServer.roomId = null;
            HomeScene.instance.OnClickBtnGoToLobby();
        });
        
        btnJoinLobby.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            OnClickJoinLobby();
        });

        btnHowToPlay.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            popupHowToPlay.ShowPopup();
        });
    }

    private void InitializeNickname()
    {
        if (nicknameInput == null)
        {
            Debug.LogWarning("Nickname input field is missing.");
            return;
        }

        string nickname = PlayerNickname.GetOrCreateNickname();
        nicknameInput.SetTextWithoutNotify(nickname);
        nicknameInput.onEndEdit.RemoveListener(OnNicknameEdited);
        nicknameInput.onEndEdit.AddListener(OnNicknameEdited);
    }

    private void OnNicknameEdited(string nickname)
    {
        string current = nickname?.Trim() ?? "";
        if (string.IsNullOrEmpty(current))
        {
            current = PlayerNickname.GetOrCreateNickname();
            nicknameInput.SetTextWithoutNotify(current);
            return;
        }

        PlayerNickname.SaveNickname(current);
        nicknameInput.SetTextWithoutNotify(PlayerNickname.GetNickname());
    }

    private void OnClickJoinLobby()
    {
        string code = roomCodeInput.text?.Trim();
        
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Room Code를 입력해주세요.");
            return;
        }
        
        // Join할 room id 저장
        LobbyServer.roomId = code;
        
        // Lobby 씬으로 이동
        SceneManager.LoadScene("LobbyScene");
    }
}

public static class PlayerNickname
{
    private const string NicknameKey = "player_nickname";

    private static readonly string[] Candidates =
    {
        "푸른파도", "하얀구름", "맑은하늘", "반짝별빛", "고요새벽", "초록숲길", "달콤바람", "은빛달", "노을빛", "햇살미소",
        "봄꽃향", "여름비", "가을잎", "겨울눈꽃", "작은파랑", "큰바람", "느린구름", "빠른번개", "고운물결", "맑은샘",
        "반달빛", "보름달", "아침이슬", "저녁노을", "산들바람", "솔잎향", "꽃잎비", "별무리", "은하수", "빛고래",
        "파도타기", "바다노래", "숲속멜로디", "하늘여행", "달빛춤", "별빛춤", "햇살요정", "구름요정", "바람요정", "물결요정",
        "봄바람", "여름햇살", "가을하늘", "겨울바람", "따뜻미소", "시원바람", "포근구름", "반짝눈", "노란해", "푸른달",
        "하늘고래", "바다고래", "숲속고래", "달고래", "별고래", "파도고래", "미소고래", "춤추는고래", "노래고래", "고래친구",
        "은빛물결", "황금노을", "푸른별", "고운달", "맑은바다", "하얀파도", "초록파도", "분홍노을", "보라하늘", "주황해",
        "아침햇살", "점심구름", "저녁별", "한밤달", "새벽별", "한낮바람", "바닷바람", "산바람", "들꽃향", "숲향기",
        "파란리본", "하늘리본", "달빛리본", "별빛리본", "노을리본", "바다리본", "꽃리본", "바람리본", "구름리본", "햇살리본",
        "맑은미소", "고운미소", "반짝미소", "달빛미소", "별빛미소", "하늘미소", "바다미소", "숲속미소", "노을미소", "햇살미소둘"
    };

    public static string GetOrCreateNickname()
    {
        if (PlayerPrefs.HasKey(NicknameKey))
        {
            string saved = Sanitize(PlayerPrefs.GetString(NicknameKey));
            if (!string.IsNullOrEmpty(saved))
            {
                return saved;
            }
        }

        string randomName = Candidates[Random.Range(0, Candidates.Length)];
        SaveNickname(randomName);
        return randomName;
    }

    public static void SaveNickname(string nickname)
    {
        string sanitized = Sanitize(nickname);
        if (string.IsNullOrEmpty(sanitized))
        {
            return;
        }

        PlayerPrefs.SetString(NicknameKey, sanitized);
        PlayerPrefs.Save();
    }

    public static string GetNickname()
    {
        return Sanitize(PlayerPrefs.GetString(NicknameKey, ""));
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        string trimmed = value.Trim();
        if (trimmed.Length > 12)
        {
            trimmed = trimmed.Substring(0, 12);
        }

        return trimmed;
    }
}

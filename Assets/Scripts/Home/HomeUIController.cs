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
    [SerializeField] private Button btnSettings;
    [SerializeField] private HomeSettingsPopup settingsPopup;

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

        btnSettings.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            
            settingsPopup.Show();
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
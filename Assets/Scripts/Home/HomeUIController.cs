using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput; 
    [SerializeField] private Button btnJoinLobby;
    [SerializeField] private Button btnCreateLobby;
    
    void Start()
    {
        btnCreateLobby.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            // RoomCode 초기화 (새로 만들 때)
            LobbyScene.roomCode = null;
            HomeScene.instance.OnClickBtnGoToLobby();
        });
        
        btnJoinLobby.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            OnClickJoinLobby();
        });
    }

    private void OnClickJoinLobby()
    {
        string code = roomCodeInput.text?.Trim();
        
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Room Code를 입력해주세요.");
            return;
        }
        
        // RoomCode 저장
        LobbyScene.roomCode = code;
        
        // Lobby 씬으로 이동
        SceneManager.LoadScene("LobbyScene");
    }
}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    public static LobbyUIController instance;
    // 0, 1 => aTeam. 2, 3 => bTeam
    [SerializeField] private LobbyPlayerCardController[] playerCards;

    [SerializeField] private Button btnGoToHome;
    [SerializeField] private Button btnStartMatching;
    [SerializeField] private TMP_Text txtTimer;
    [SerializeField] private TMP_Text txtRoomCode;
    [SerializeField] private Button btnCopyRoomCode;

    private Player[] players = new Player[4];
    private bool[] isPlayerSet = new bool[4];
    private string currentRoomCode;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // 매칭 시작 버튼
        btnStartMatching.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");

            // 서버로 start_matching 메시지 전송
            if (LobbyServer.instance != null)
            {
                LobbyServer.instance.SendStartMatchingMessage();
            }
            
            // 매칭 버튼 비활성화
            btnStartMatching.interactable = false;
            // 매칭 타이머 보여주기
            txtTimer.gameObject.SetActive(true);
        });

        // 홈으로 가기 버튼
        btnGoToHome.onClick.AddListener(() => {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            GoToHome();
        });

        // 룸 코드 복사 버튼
        if (btnCopyRoomCode != null)
        {
            btnCopyRoomCode.onClick.AddListener(() => {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySfx("Btn_Click");
                CopyRoomCodeToClipboard();
            });
        }

        // 룸 접속 후 0.5초 동안 매칭 시작 버튼 비활성화
        if (btnStartMatching != null)
        {
            btnStartMatching.interactable = false;
            StartCoroutine(EnableStartMatchingAfterDelay(0.5f));
        }
    }

    public void UpdateTimer(float time)
    {
        if (txtTimer != null)
        {
            txtTimer.text = $"{time:F0}";
        }
    }

    /// <summary>
    /// 매칭 타이머를 보이게 하고 초기값으로 설정합니다.
    /// </summary>
    public void ShowAndResetMatchingTimer()
    {
        if (txtTimer != null)
        {
            txtTimer.gameObject.SetActive(true);
            txtTimer.text = "0";
            LobbyScene.instance.StartMatching();
        }
    }

    private void GoToHome()
    {
        // 룸에서 나가기
        if (LobbyServer.instance != null)
        {
            LobbyServer.instance.LeaveRoom();
        }
        
        // HomeScene으로 이동
        SceneManager.LoadScene("HomeScene");
    }

    public void SetPlayerCard(Player player)
    {
        Debug.Log("SetPlayerCard " + player.sessionId);
        // 이미 있는 플레이어인지 확인. 있으면 세팅 진행x
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && player.sessionId == players[i].sessionId)
            {
                Debug.Log(player.sessionId + " 는 이미 세팅되어 있습니다.");
                return;
            }
        }

        for (int i = 0; i < isPlayerSet.Length; i++)
        {
            if (isPlayerSet[i] == false)
            {
                isPlayerSet[i] = true;
                playerCards[i].SetName(player.name);
                playerCards[i].SetActive(true);
                players[i] = player;
                break;
            }
        }
    }

    public void RemovePlayerCard(Player player)
    {
        Debug.Log("RemovePlayerCard " + player.sessionId);
        
        // 해당 플레이어를 찾아서 제거
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && player.sessionId == players[i].sessionId)
            {
                // 플레이어 카드 UI 숨기기 또는 제거
                if (playerCards[i] != null)
                {
                    playerCards[i].SetName("-"); // 이름 초기화
                    playerCards[i].SetActive(false);
                    // 또는 playerCards[i].gameObject.SetActive(false);
                }
                
                // 배열에서 제거
                isPlayerSet[i] = false;
                players[i] = null;
                
                Debug.Log($"Player {player.sessionId} removed from slot {i}");
                return;
            }
        }
        
        Debug.LogWarning($"Player {player.sessionId} not found in player list");
    }

    public void ClearPlayerCards()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (playerCards[i] != null)
            {
                playerCards[i].SetName("-"); // 이름 초기화
                playerCards[i].SetActive(false);
            }
            isPlayerSet[i] = false;
            players[i] = null;
        }
    }

    /// <summary>
    /// 룸 코드를 설정하고 UI에 표시합니다.
    /// 룸 접속 후 1초 동안은 매칭 시작 버튼을 비활성화합니다.
    /// </summary>
    /// <param name="roomCode">표시할 룸 코드</param>
    public void SetRoomCode(string roomCode)
    {
        currentRoomCode = roomCode;
        
        if (txtRoomCode != null)
        {
            txtRoomCode.text = roomCode ?? "";
        }
        
        // 룸 코드가 있으면 복사 버튼 활성화
        if (btnCopyRoomCode != null)
        {
            btnCopyRoomCode.interactable = !string.IsNullOrEmpty(roomCode);
        }
    }

    /// <summary>
    /// sessionId와 roomOwnerSessionId를 비교해 매칭 시작 버튼을 보이거나 숨깁니다.
    /// 방장만 버튼이 보입니다.
    /// </summary>
    public void RefreshBtnStartMatchingVisibility()
    {
        if (btnStartMatching == null) return;
        
        StartCoroutine(CoRefreshBtnStartMatchingVisibility());
    }

    private IEnumerator CoRefreshBtnStartMatchingVisibility()
    {
        yield return new WaitForSeconds(0.2f); // room 상태 업데이트 지연 시간 대기

        string mySessionId = LobbyServer.sessionId;
        string ownerSessionId = LobbyServer.RoomOwnerSessionId;
        bool isOwner = !string.IsNullOrEmpty(ownerSessionId) && mySessionId == ownerSessionId;
        btnStartMatching.gameObject.SetActive(isOwner);
    }

    private IEnumerator EnableStartMatchingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (btnStartMatching != null)
        {
            btnStartMatching.interactable = true;
        }
    }

    /// <summary>
    /// 현재 룸 코드를 클립보드에 복사합니다.
    /// </summary>
    private void CopyRoomCodeToClipboard()
    {
        if (string.IsNullOrEmpty(currentRoomCode))
        {
            Debug.LogWarning("Room code is empty, cannot copy to clipboard");
            return;
        }

        GUIUtility.systemCopyBuffer = currentRoomCode;
        Debug.Log($"Room code copied to clipboard: {currentRoomCode}");
        
        // 복사 성공 피드백 (선택사항)
        // 예: 토스트 메시지 표시 등
    }
}

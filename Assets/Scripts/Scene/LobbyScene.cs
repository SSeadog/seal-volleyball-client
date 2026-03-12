using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyScene : MonoBehaviour
{
    public static LobbyScene instance;
    
    // RoomCode 저장용 static 변수 (씬 전환 시에도 유지됨)
    public static string roomCode { get; set; }

    [SerializeField] private GameObject matchingEndUI;

    private float matchingTimer = 0f;

    private bool isMatching = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // RoomCode가 있으면 해당 룸으로 접속, 없으면 기본 룸으로 접속
        if (!string.IsNullOrEmpty(roomCode))
        {
            // RoomCode를 사용하여 룸 접속 로직
            Debug.Log($"Attempting to join room with code: {roomCode}");
            
            // roomCode를 roomId로 사용하여 해당 룸에 접속 시도
            // JoinRoomById는 async void이므로 직접 호출
            // 에러 발생 시 OnError 이벤트에서 처리하거나, 실패 시 자동으로 새 룸 생성
            LobbyServer.instance.JoinRoomById(roomCode);
            
            roomCode = null;
        }
        else
        {
            // 기본 룸 접속
            LobbyServer.instance.CreateRoom();
        }
        
        // 사용 후 RoomCode 초기화 (선택사항)
        // RoomCode = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isMatching) return;

        // 서버에서 타이머 값 받아올 예정
        // 종료 타이밍 또한 서버에서 받을 예정
        matchingTimer += Time.deltaTime;
        
        if (LobbyUIController.instance != null)
        {
            LobbyUIController.instance.UpdateTimer(matchingTimer);
        }

        if (matchingTimer <= 0)
        {
            isMatching = false;
            StartCoroutine(EndMatching());
        }
    }

    public void StartMatching()
    {
        isMatching = true;
    }

    private IEnumerator EndMatching()
    {
        // 서버에서 매칭 종료 신호 받으면 실행할 로직
        // 매칭 종료 ui 보여주기
        matchingEndUI.SetActive(true);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("VolleyBallScene");
    }
}

using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string sessionId;
    [SerializeField] private int playerIndex;
    [SerializeField] private bool isMine;
    
    [Header("Sync Settings")]
    [SerializeField] private float interpolationSpeed = 10f;
    private float serverPosX = float.MinValue;
    private float serverPosY = float.MinValue;
    private bool hasServerPosition = false;
    
    /// <summary>
    /// PlayerView를 초기화합니다.
    /// </summary>
    /// <param name="index">플레이어 인덱스</param>
    /// <param name="playerSessionId">플레이어 세션 ID</param>
    /// <param name="isMyPlayer">내 플레이어인지 여부</param>
    public void Initialize(int index, string playerSessionId, bool isMyPlayer = false)
    {
        playerIndex = index;
        sessionId = playerSessionId;
        isMine = isMyPlayer;
        
        // VolleyBallPlayerManager에 등록
        if (VolleyBallPlayerManager.instance != null)
        {
            VolleyBallPlayerManager.instance.RegisterPlayerView(sessionId, this);
        }
    }
    
    /// <summary>
    /// 서버에서 받은 위치를 업데이트합니다.
    /// </summary>
    /// <param name="posX">서버 X 좌표</param>
    /// <param name="posY">서버 Y 좌표</param>
    public void UpdatePosition(float posX, float posY)
    {
        serverPosX = posX;
        serverPosY = posY;
        hasServerPosition = true;
    }
    
    void Update()
    {
        if (hasServerPosition)
        {
            if (playerIndex == 1) // 테스트용 디버그
            {
                Debug.Log($"[PlayerView] playerIndex=1 serverPosX={serverPosX}");
            }
            
            // 서버 위치로 보간
            Vector3 clientPosition = transform.position;
            Vector3 targetPosition = new Vector3(serverPosX, serverPosY, clientPosition.z);
            
            transform.position = Vector3.Lerp(clientPosition, targetPosition, Time.deltaTime * interpolationSpeed);
        }
    }
    
    void OnDestroy()
    {
        // VolleyBallPlayerManager에서 등록 해제
        if (VolleyBallPlayerManager.instance != null)
        {
            VolleyBallPlayerManager.instance.UnregisterPlayerView(sessionId);
        }
    }
    
    public int PlayerIndex => playerIndex;
    public string SessionId => sessionId;
    public bool IsMine => isMine;
}


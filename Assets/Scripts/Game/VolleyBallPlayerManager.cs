using UnityEngine;
using System.Collections.Generic;

// 팀 할당 로직 개선 필요

public class VolleyBallPlayerManager : MonoBehaviour
{
    public static VolleyBallPlayerManager instance;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPlayerPrefab;
    
    [SerializeField] private Transform[] playerSpawnPositions;
    [SerializeField] private Color[] playerColors;

    [SerializeField] private Transform netTransform;

    private int currentPlayerIndex = 0;
    
    // sessionId 기준으로 PlayerView를 관리
    private Dictionary<string, PlayerView> playerViewsBySessionId = new Dictionary<string, PlayerView>();

    private void Awake()
    {
        instance = this;
    }

    // 기본 스폰: 내부 인덱스를 사용 (세션 ID는 알 수 없으므로 null)
    public GameObject SpawnPlayer(bool isAi)
    {
        var player = SpawnPlayer(isAi, currentPlayerIndex, null, null);
        currentPlayerIndex++;
        return player;
    }

    // 지정된 인덱스로 스폰
    // 닉네임 라벨 표시까지 포함해서 스폰
    public GameObject SpawnPlayer(bool isAi, int index, string sessionId, string playerName)
    {
        GameObject playerInstance;

        if (isAi == true)
        {
            playerInstance = Instantiate(aiPlayerPrefab);
        }
        else
        {
            playerInstance = Instantiate(playerPrefab);
        }

        Debug.Log($"SpawnPlayer index: {index}");
        playerInstance.GetComponentInChildren<SpriteRenderer>().color = playerColors[index];
        playerInstance.transform.position = playerSpawnPositions[index].position;
        playerInstance.transform.rotation = playerSpawnPositions[index].rotation;

        // 내 플레이어인지 여부 (AI는 항상 false)
        bool isMyPlayer = !isAi && sessionId == GameServer.sessionId;

        // 팀 방향 및 로컬 여부에 따라 PlayerController 초기화
        bool isLeftTeam = index < 2; // 0,1 인덱스: leftTeam / 2,3: rightTeam
        playerInstance.GetComponent<PlayerController>().Init(netTransform, isLeftTeam, isMyPlayer);

        // 내 플레이어가 아니고 AI 플레이어가 아니면 PlayerInputHandler 비활성화 및 PlayerView 추가 (원격 플레이어)
        Debug.Log("sessionId: " + sessionId + ", isMyPlayer: " + isMyPlayer);
        if (isMyPlayer)
        {
            PlayerInputHandler inputHandler = playerInstance.GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                inputHandler.enabled = true;
                if (VolleyBallSceneUIController.instance != null)
                    VolleyBallSceneUIController.instance.InitializePlayerInputPanel(inputHandler);
            }

            PlayerView playerView = playerInstance.GetComponent<PlayerView>();
            playerView.Initialize(index, sessionId, isMyPlayer);
        }
        else
        {
            // PlayerView 추가 및 초기화
            PlayerView playerView = playerInstance.GetComponent<PlayerView>();
            // GameServer에서 전달받은 sessionId 사용 (없으면 null 또는 빈 문자열 가능)
            // isMyPlayer는 false (원격 플레이어이므로)
            playerView.Initialize(index, sessionId, isMyPlayer);
        }

        // 닉네임 표시(프리팹에 있는 TextMeshPro에 값 세팅)
        string nickname = playerName;
        if (string.IsNullOrEmpty(nickname) && isMyPlayer)
            nickname = PlayerNickname.GetNickname();

        PlayerView pv = playerInstance.GetComponent<PlayerView>();
        if (pv != null)
            pv.SetNickname(nickname);

        return playerInstance;
    }

    /// <summary>
    /// sessionId가 이미 등록된 플레이어가 있는지 여부를 반환합니다.
    /// </summary>
    public bool HasPlayerView(string sessionId)
    {
        return !string.IsNullOrEmpty(sessionId) && playerViewsBySessionId.ContainsKey(sessionId);
    }

    /// <summary>
    /// sessionId로 PlayerView를 등록합니다.
    /// </summary>
    public void RegisterPlayerView(string sessionId, PlayerView view)
    {
        if (string.IsNullOrEmpty(sessionId) || view == null) return;
        playerViewsBySessionId[sessionId] = view;
    }

    /// <summary>
    /// sessionId에 해당하는 플레이어의 PlayerController를 반환합니다.
    /// </summary>
    public PlayerController GetPlayerControllerBySessionId(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null;
        if (!playerViewsBySessionId.TryGetValue(sessionId, out var view) || view == null) return null;
        return view.GetComponent<PlayerController>();
    }

    /// <summary>
    /// sessionId로 PlayerView 등록을 해제합니다.
    /// </summary>
    public void UnregisterPlayerView(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;
        if (playerViewsBySessionId.ContainsKey(sessionId))
        {
            playerViewsBySessionId.Remove(sessionId);
        }
    }

    /// <summary>
    /// sessionId 기준으로 원격 플레이어 위치를 업데이트합니다.
    /// </summary>
    public void UpdatePlayerViewPosition(string sessionId, float posX, float posY)
    {
        if (string.IsNullOrEmpty(sessionId)) return;
        if (!playerViewsBySessionId.TryGetValue(sessionId, out var view) || view == null) return;

        view.UpdatePosition(posX, posY);
    }
}

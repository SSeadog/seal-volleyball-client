using Colyseus;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class GameServer : ServerBase<GameRoomState>
{
    public new static GameServer instance => ServerBase<GameRoomState>.instance as GameServer;
    
    public static string roomId;
    public static string lobbyRoomId;

    public static int myPlayerIndex;
    public static int myTeamIndex;
    
    // 메시지 핸들러를 저장하여 나중에 참조할 수 있도록 함
    private Action<Player> playerJoinedHandler;
    private Action<Player> playerLeftHandler;
    
    // volleyBall 위치 추적을 위한 변수
    private float serverPosX = float.MinValue;
    private float serverPosY = float.MinValue;
    private bool hasServerPosition = false; // 서버 위치가 설정되었는지 확인

    [Header("Local Player Position Sync")]
    [SerializeField] private float localPlayerPositionCorrectionFactor = 0.1f;
    private Vector2 localPlayerPositionError = Vector2.zero;
    private bool hasLocalPlayerPositionError = false;

    // ping/pong 기반 레이턴시 측정용
    private long lastPingSentTimeMs = 0;
    private float lastLatencyMs = 0f;

    public static float LatencyMs
    {
        get
        {
            var g = instance;
            return g != null ? g.lastLatencyMs : 0f;
        }
    }

    // 클라이언트 입력 동기화를 위한 fixed tick
    private int fixedTickCount = 0;
    private int startClientTime = 0;
    private long startUtcServerTime = 0;
    private long calculatedUtcServerTime = 0;
    private bool isGameStarted = false;

    // VolleyBallView에서 참조할 수 있도록 공개 프로퍼티 제공
    public static bool HasBallServerPosition
    {
        get
        {
            var g = instance;
            return g != null && g.hasServerPosition;
        }
    }

    public static float ServerBallPosX
    {
        get
        {
            var g = instance;
            return g != null ? g.serverPosX : 0f;
        }
    }

    public static float ServerBallPosY
    {
        get
        {
            var g = instance;
            return g != null ? g.serverPosY : 0f;
        }
    }

    /// <summary> 룸 상태의 게임 시작 시각 (UTC milliseconds). 타이머 표시용. </summary>
    public static double GameStartTimeUtcMs
    {
        get
        {
            var g = instance;
            if (g?.room?.State == null) return 0;
            // return (double)g.room.State.gameStartTime;
            return g.startUtcServerTime;
        }
    }

    /// <summary> 서버 상태 기준 왼쪽 팀 점수 </summary>
    public static int LeftTeamScore
    {
        get
        {
            var g = instance;
            if (g?.room?.State == null) return 0;
            return (int)g.room.State.leftTeamScore;
        }
    }

    /// <summary> 서버 상태 기준 오른쪽 팀 점수 </summary>
    public static int RightTeamScore
    {
        get
        {
            var g = instance;
            if (g?.room?.State == null) return 0;
            return (int)g.room.State.rightTeamScore;
        }
    }
    
    [SerializeField] private float interpolationSpeed = 10f; // 보간 속도 (볼용)
    [SerializeField] private float debugSphereRadius = 0.2f; // Debug sphere 반지름

    protected override void Awake()
    {
        
        base.Awake();

        roomName = "game_room";

        Time.timeScale = 0f;

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerJoined += OnPlayerJoinedHandler;
        
        OnPlayerLeft -= OnPlayerLeftHandler;
        OnPlayerLeft += OnPlayerLeftHandler;
    }

    void Start()
    {
        Debug.Log("GameServer Start");
        
        // LobbyServer의 sessionId를 options에 포함하여 전송
        Dictionary<string, object> options = null;
        if (!string.IsNullOrEmpty(sessionId))
        {
            options = new Dictionary<string, object>
            {
                { "sessionId", sessionId },
                { "playerIndex", myPlayerIndex },
                { "teamIndex", myTeamIndex},
                { "lobbyRoomId", lobbyRoomId},
                { "nickname", PlayerNickname.GetNickname() }
            };
            Debug.Log($"Joining game room with sessionId: {sessionId}");
        }
        
        JoinRoomById(roomId, options);
    }
    
    private void DrawDebugSphere(Vector3 center, float radius, Color color)
    {
        // XY 평면 원
        DrawCircle(center, radius, Vector3.forward, color, 32);
        // XZ 평면 원
        DrawCircle(center, radius, Vector3.up, color, 32);
        // YZ 평면 원
        DrawCircle(center, radius, Vector3.right, color, 32);
    }
    
    private void DrawCircle(Vector3 center, float radius, Vector3 normal, Color color, int segments)
    {
        Vector3 forward = Vector3.Slerp(-normal, normal, 0.5f);
        Vector3 right = Vector3.Cross(normal, forward).normalized * radius;
        Vector3 up = Vector3.Cross(right, normal).normalized * radius;
        
        Vector3 prevPoint = center + right;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 point = center + right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            Debug.DrawLine(prevPoint, point, color, 1f);
            prevPoint = point;
        }
    }


    protected override void RegisterRoomEvents(ColyseusRoom<GameRoomState> room)
    {
        base.RegisterRoomEvents(room);
        
        // GameServer에서 사용하는 메시지 핸들러 등록
        // 핸들러를 변수에 저장하여 나중에 참조 가능하도록 함
        playerJoinedHandler = (player) =>
        {
            // 인스턴스가 파괴되었는지 확인
            if (this == null || room == null) return;
            Debug.Log("player Joined! " + player.sessionId);
            OnPlayerJoined?.Invoke(player);
        };
        
        playerLeftHandler = (player) =>
        {
            // 인스턴스가 파괴되었는지 확인
            if (this == null || room == null) return;
            Debug.Log("player Left! " + player.sessionId);
            OnPlayerLeft?.Invoke(player);
        };
        
        room.OnMessage<Player>("playerJoined", playerJoinedHandler);
        room.OnMessage<Player>("playerLeft", playerLeftHandler);
        
        room.OnMessage<long>("game_start", (time) => {
            // 게임 시작 처리
            Time.timeScale = 1f;

            // serverTime 초기화 및 카운트 시작
            startClientTime = (int)(Time.realtimeSinceStartup * 1000f);
            startUtcServerTime = time;
            calculatedUtcServerTime = time;
            isGameStarted = true;

            // 볼 활성화
            if (VolleyBallGameManager.instance != null)
            {
                VolleyBallGameManager.instance.ActivateBall();
            }

            Debug.Log($"game_start 메시지 받음!! 클라 utc: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} 서버 utc: {time} latency: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - time}");
        });

        // 'pong' 메시지: ping 왕복 시간을 이용해 레이턴시(ms) 캐싱
        room.OnMessage<object>("pong", _ =>
        {
            Debug.Log("[GameServer] receive pong");
            if (this == null || room == null) return;
            if (lastPingSentTimeMs <= 0) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long rtt = now - lastPingSentTimeMs;
            if (rtt > 0)
            {
                lastLatencyMs = rtt * 0.5f; // 편도 레이턴시 추정
            }
        });
        
        // 'game_end' 메시지: 승리 팀 결과 표시
        room.OnMessage<Dictionary<string, object>>("game_ended", (message) =>
        {
            if (this == null || room == null) return;
            
            if (VolleyBallSceneUIController.instance != null)
            {
                int winTeam = int.Parse(message["winTeam"].ToString());
                VolleyBallSceneUIController.instance.ShowResultPanel(winTeam);
            }

            if (VolleyBallGameManager.instance != null)
            {
                // 게임 종료 플래그
                VolleyBallGameManager.instance.EndGame();
            }
        });

        // 게임에서 로비로 돌아올 때 클라이언트가 보내는 lobbyRoomId 반영
        room.OnMessage<Dictionary<string, object>>("return_to_lobby", OnReturnToLobby);

        // 'ground' 메시지: 볼 색상을 검은색으로 변경
        room.OnMessage<object>("ground", (message) => {
            if (this == null || room == null) return;
            Debug.Log("Received 'ground' message - changing ball color to black");
            SetBallColor(Color.black);
        });
        
        // 'resetBall' 메시지: 볼 색상을 흰색으로 변경
        room.OnMessage<object>("resetBall", (message) => {
            if (this == null || room == null) return;
            Debug.Log("Received 'resetBall' message - changing ball color to white");
            SetBallColor(Color.white);
        });

        // 서버에서 'toss', 'receive', 'spike' 메시지: sessionId로 플레이어 찾아 애니 재생 + 손 위치 이펙트 (본인은 이미 로컬에서 재생하므로 제외)
        room.OnMessage<Dictionary<string, object>>("toss", handMessage =>
        {
            if (this == null || room == null) return;
            if (TryGetSessionId(handMessage, out var sid) && VolleyBallPlayerManager.instance != null)
            {
                var pc = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(sid);
                var pv = pc != null ? pc.GetComponent<PlayerView>() : null;
                bool isLocalPlayer = pv != null && pv.IsMine;
                if (pc != null && !isLocalPlayer)
                {
                    pc.PlayTossAnimation();
                    SoundManager.Instance.PlaySfx("Seal_Toss");
                }
            }
            if (TryGetHandPosition(handMessage, out var handPos))
            {
                DrawDebugSphere(handPos, PhysicsConstants.PLAYER_HAND_CHECK_RADIUS, Color.green);
                if (VolleyBallScene.Instance != null) VolleyBallScene.Instance.SpawnTossHandEffect(handPos);
            }
        });

        room.OnMessage<Dictionary<string, object>>("receive", handMessage =>
        {
            if (this == null || room == null) return;
            if (TryGetSessionId(handMessage, out var sid) && VolleyBallPlayerManager.instance != null)
            {
                var pc = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(sid);
                var pv = pc != null ? pc.GetComponent<PlayerView>() : null;
                bool isLocalPlayer = pv != null && pv.IsMine;
                if (pc != null && !isLocalPlayer)
                {
                    pc.PlayReceiveAnimation();
                    SoundManager.Instance.PlaySfx("Seal_Dig");
                }
            }
            if (TryGetHandPosition(handMessage, out var handPos))
            {
                DrawDebugSphere(handPos, PhysicsConstants.PLAYER_HAND_CHECK_RADIUS, Color.blue);
                if (VolleyBallScene.Instance != null) VolleyBallScene.Instance.SpawnReceiveHandEffect(handPos);
            }
        });

        room.OnMessage<Dictionary<string, object>>("spike", handMessage =>
        {
            if (this == null || room == null) return;
            if (TryGetSessionId(handMessage, out var sid) && VolleyBallPlayerManager.instance != null)
            {
                var pc = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(sid);
                var pv = pc != null ? pc.GetComponent<PlayerView>() : null;
                bool isLocalPlayer = pv != null && pv.IsMine;
                if (pc != null && !isLocalPlayer)
                {
                    pc.PlaySpikeAnimation();
                    SoundManager.Instance.PlaySfx("Seal_Spike");
                }
            }
            if (TryGetHandPosition(handMessage, out var handPos))
            {
                DrawDebugSphere(handPos, PhysicsConstants.PLAYER_HAND_CHECK_RADIUS, Color.red);
                if (VolleyBallScene.Instance != null) VolleyBallScene.Instance.SpawnSpikeHandEffect(handPos);
            }
        });

        room.OnMessage<Dictionary<string, object>>("jump", message =>
        {
            if (this == null || room == null) return;
            if (!TryGetSessionId(message, out var sid) || VolleyBallPlayerManager.instance == null) return;
            var pc = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(sid);
            var pv = pc != null ? pc.GetComponent<PlayerView>() : null;
            if (pc != null && (pv == null || !pv.IsMine)) pc.PlayJumpAnimation();
        });

        room.OnMessage<int>("judge", message =>
        {
            if (this == null || room == null) return;
            if (VolleyBallSceneUIController.instance != null)
                VolleyBallSceneUIController.instance.ShowJudgeMessage(message);

            SetBallColor(Color.black);
        });
        
        // volleyBall 위치 변경 감지, player 위치 변경 감지를 위한 OnStateChange 이벤트 추가 등록
        room.OnStateChange += OnGameStateChanged;
    }

    protected override bool IsRoomReadyForOnJoinRoom()
    {
        if (!base.IsRoomReadyForOnJoinRoom()) return false;
        // GameServer는 OnJoinRoom()에서 players를 순회하므로, players가 준비될 때까지 추가로 기다림
        return room.State.players != null;
    }

    /// <summary>
    /// 서버에서 전달된 메시지에서 sessionId를 추출합니다.
    /// </summary>
    private bool TryGetSessionId(Dictionary<string, object> message, out string sessionId)
    {
        sessionId = null;
        if (message == null || !message.TryGetValue("sessionId", out var obj) || obj == null) return false;
        sessionId = obj.ToString();
        return !string.IsNullOrEmpty(sessionId);
    }

    /// <summary>
    /// 서버에서 전달된 handMessage(DTO)에서 손 좌표를 추출합니다.
    /// </summary>
    private bool TryGetHandPosition(Dictionary<string, object> message, out Vector3 handPos)
    {
        handPos = Vector3.zero;
        if (message == null) return false;

        if (!message.TryGetValue("handX", out var xObj) ||
            !message.TryGetValue("handY", out var yObj) ||
            xObj == null || yObj == null)
        {
            return false;
        }

        float x, y;

        if (xObj is double dx) x = (float)dx;
        else if (xObj is float fx) x = fx;
        else if (!float.TryParse(xObj.ToString(), out x)) return false;

        if (yObj is double dy) y = (float)dy;
        else if (yObj is float fy) y = fy;
        else if (!float.TryParse(yObj.ToString(), out y)) return false;

        handPos = new Vector3(x, y, 0f);
        return true;
    }

    private void FixedUpdate()
    {
        // 게임 시작 후부터 fixedTickCount를 증가시킴
        if (isGameStarted)
        {
            fixedTickCount = (int)((Time.realtimeSinceStartup * 1000f - startClientTime) / 16);
        }

        // 로컬 플레이어 위치 오차 보정
        if (hasLocalPlayerPositionError && VolleyBallPlayerManager.instance != null && !string.IsNullOrEmpty(sessionId))
        {
            var localPlayerController = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(sessionId);
            if (localPlayerController != null)
            {
                // 한 번에 오차의 일부만 보정
                Vector2 correction = localPlayerPositionError * localPlayerPositionCorrectionFactor;

                var currentPos = localPlayerController.transform.position;
                currentPos.x += correction.x;
                currentPos.y += correction.y;
                localPlayerController.transform.position = currentPos;

                // 남은 오차 갱신
                localPlayerPositionError -= correction;
                if (localPlayerPositionError.sqrMagnitude < 0.0001f)
                {
                    hasLocalPlayerPositionError = false;
                    localPlayerPositionError = Vector2.zero;
                }
            }
        }
    }
    
    private void OnGameStateChanged(GameRoomState state, bool isFirstState)
    {
        if (this == null || room == null || state == null) return;
        
        // volleyBall의 posX, posY가 변경되었는지 확인
        if (state.volleyBall != null)
        {
            VolleyBall volleyBall = state.volleyBall;
            if (volleyBall.posX != serverPosX || volleyBall.posY != serverPosY)
            {
                serverPosX = volleyBall.posX;
                serverPosY = volleyBall.posY;
                hasServerPosition = true;
                
                // 서버 위치를 빨간색 Debug sphere로 표시
                Vector3 serverPosition = new Vector3(serverPosX, serverPosY, 0f);
                DrawDebugSphere(serverPosition, debugSphereRadius, Color.red);
            }
        }
        
        // 플레이어 위치 업데이트 (PlayerView에 전달 / 로컬 플레이어는 오차 캐싱)
        if (state.players != null)
        {
            for (int i = 0; i < state.players.Count; i++)
            {
                Player player = state.players[i];
                if (VolleyBallPlayerManager.instance == null) continue;

                // 로컬 플레이어 처리: 현재 위치와 서버 위치의 오차를 캐싱
                if (player.sessionId == sessionId)
                {
                    var localPlayerController = VolleyBallPlayerManager.instance.GetPlayerControllerBySessionId(player.sessionId);
                    if (localPlayerController != null)
                    {
                        Vector3 currentPos = localPlayerController.transform.position;
                        float errorX = player.posX - currentPos.x;
                        float errorY = player.posY - currentPos.y;
                        localPlayerPositionError = new Vector2(errorX, errorY);
                        hasLocalPlayerPositionError = true;
                    }
                }
                else
                {
                    Debug.Log($"On Position Change sessionId: {player.sessionId} playerIndex: {player.playerIndex} posX: {player.posX}");
                    VolleyBallPlayerManager.instance.UpdatePlayerViewPosition(player.sessionId, player.posX, player.posY);
                }
            }
        }
    }
    

    protected override void OnJoinRoom()
    {
        base.OnJoinRoom();
        
        // 각종 준비요소 준비 후
        // 서버로 ready 메시지 보내기
        Debug.Log("send ready to server");

        // Debug.Log($"OnJoinRoom player count: {room.State.players.Count}");

        // 서버에서 전달받은 myPlayerIndex를 기준으로 로컬 플레이어 스폰 (이미 스폰된 sessionId는 제외)
        if (VolleyBallPlayerManager.instance != null)
        {
            if (room?.State?.players == null)
            {
                Debug.LogWarning("[GameServer] OnJoinRoom called but State.players is not ready yet.");
                return;
            }

            for (int i = 0; i < room.State.players.Count; i++)
            {
                string sid = room.State.players[i].sessionId;
                if (VolleyBallPlayerManager.instance.HasPlayerView(sid))
                    continue;

                Debug.Log($"plaer[{i}]: {sid}");
                VolleyBallPlayerManager.instance.SpawnPlayer(
                    room.State.players[i].isAI,
                    (int)room.State.players[i].playerIndex,
                    sid,
                    room.State.players[i].name
                );
            }
        }

        // 초기 volleyBall 위치 설정
        if (room.State.volleyBall != null)
        {
            VolleyBall volleyBall = room.State.volleyBall;
            serverPosX = volleyBall.posX;
            serverPosY = volleyBall.posY;
            hasServerPosition = true;
        }

        room.Send("ready");

        // 1초마다 ping 전송
        StartCoroutine(CoPing());
    }

    private IEnumerator CoPing()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (room != null)
            {
                SendPing();
            }
        }
    }

    /// <summary>
    /// 서버로 'ping' 메시지를 보내고 레이턴시 계산을 위한 전송 시간(ms)을 기록합니다.
    /// </summary>
    public void SendPing()
    {
        if (room == null) return;
        lastPingSentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Debug.Log($"Send ping to roomName={room.Name}, roomId={room.RoomId}");
        room.Send("ping");
    }

    private void OnPlayerJoinedHandler(Player player)
    {
        Debug.Log($"GameServer: Player {player.sessionId} joined");
        if (player.sessionId == sessionId) return;
        if (VolleyBallPlayerManager.instance == null) return;
        if (VolleyBallPlayerManager.instance.HasPlayerView(player.sessionId))
            return;
        VolleyBallPlayerManager.instance.SpawnPlayer(
            player.isAI,
            (int)player.playerIndex,
            player.sessionId,
            player.name
        );
    }

    private void OnPlayerLeftHandler(Player player)
    {
        // GameServer에서 플레이어가 나갔을 때 처리할 로직
        Debug.Log($"GameServer: Player {player.sessionId} left");
        // 여기에 필요한 로직 추가 (예: 플레이어 제거, UI 업데이트 등)
    }
    
    public async void SendInputMessage(PlayerInputData input)
    {
        if (room == null) return;

        // 현재 fixedTickCount를 tick에 설정하여 서버와 입력을 동기화
        // 레이턴시 고려하여 tick 증가(레이턴시 / deltaTime)
        input.tick = fixedTickCount + (int)(lastLatencyMs / PhysicsConstants.DELTA_TIME);

        await room.Send(0, input);
        Debug.Log($"GameServer: Sent 0 message with input {input} correction: {lastLatencyMs / PhysicsConstants.DELTA_TIME}");
    }

    /// <summary>
    /// 서버로 'toss' 메시지를 보냅니다.
    /// </summary>
    /// <param name="handX">손 위치 X</param>
    /// <param name="handY">손 위치 Y</param>
    public async void SendTossMessage(float handX, float handY)
    {
        if (room == null)
        {
            Debug.LogWarning("GameServer: Cannot send toss message - not connected to room");
            return;
        }
        
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "handX", handX },
            { "handY", handY }
        };
        
        // await room.Send("toss", message);
        Debug.Log($"GameServer: Sent toss message with hand position ({handX}, {handY})");
    }
    
    /// <summary>
    /// 서버로 'receive' 메시지를 보냅니다.
    /// </summary>
    /// <param name="handX">손 위치 X</param>
    /// <param name="handY">손 위치 Y</param>
    public async void SendReceiveMessage(float handX, float handY)
    {
        if (room == null)
        {
            Debug.LogWarning("GameServer: Cannot send receive message - not connected to room");
            return;
        }
        
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "handX", handX },
            { "handY", handY }
        };
        
        // await room.Send("receive", message);
        Debug.Log($"GameServer: Sent receive message with hand position ({handX}, {handY})");
    }
    
    /// <summary>
    /// 서버로 'spike' 메시지를 보냅니다.
    /// </summary>
    /// <param name="handX">손 위치 X</param>
    /// <param name="handY">손 위치 Y</param>
    public async void SendSpikeMessage(float handX, float handY)
    {
        if (room == null)
        {
            Debug.LogWarning("GameServer: Cannot send spike message - not connected to room");
            return;
        }
        
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "handX", handX },
            { "handY", handY }
        };
        
        // await room.Send("spike", message);
        Debug.Log($"GameServer: Sent spike message with hand position ({handX}, {handY})");
    }
    
    /// <summary>
    /// 볼의 SpriteRenderer 색상을 변경합니다.
    /// </summary>
    /// <param name="color">변경할 색상</param>
    private void SetBallColor(Color color)
    {
        if (VolleyBallGameManager.instance == null) return;
        
        Transform ballTransform = VolleyBallGameManager.instance.GetVolleyBall();
        if (ballTransform == null) return;
        
        SpriteRenderer spriteRenderer = ballTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // 자식 오브젝트에서 찾기
            spriteRenderer = ballTransform.GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            Debug.Log($"GameServer: Ball color changed to {color}");
        }
        else
        {
            Debug.LogWarning("GameServer: SpriteRenderer not found on ball");
        }
    }

    protected override void OnLeaveRoom(int code)
    {
        // 룸을 null로 설정하여 메시지 핸들러가 호출되어도 안전하게 처리
        if (room != null)
        {
            room.OnStateChange -= OnGameStateChanged;
            room = null;
        }

        base.OnLeaveRoom(code);

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerLeft -= OnPlayerLeftHandler;
        
        // 핸들러 참조 제거
        playerJoinedHandler = null;
        playerLeftHandler = null;
        
        // 위치 추적 변수 초기화
        serverPosX = float.MinValue;
        serverPosY = float.MinValue;
        hasServerPosition = false;
    }

    private void OnReturnToLobby(Dictionary<string, object> message)
    {
        Debug.Log("[GameServer] return_to_lobby 메시지 받음");
        if (this == null || room == null) return;

        if (message == null) return;

        if (message.TryGetValue("lobbyRoomId", out var id) && id != null)
        {
            LobbyServer.roomId = id.ToString();
            Debug.Log($"LobbyServer.roomId set from return_to_lobby: {LobbyServer.roomId}");
        }
    }

    protected override void OnDestroy()
    {
        // 룸을 먼저 null로 설정
        if (room != null)
        {
            room.OnStateChange -= OnGameStateChanged;
        }

        base.OnDestroy();

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerLeft -= OnPlayerLeftHandler;
        
        // 핸들러 참조 제거
        playerJoinedHandler = null;
        playerLeftHandler = null;
        
        // 위치 추적 변수 초기화
        serverPosX = float.MinValue;
        serverPosY = float.MinValue;
        hasServerPosition = false;
    }
}

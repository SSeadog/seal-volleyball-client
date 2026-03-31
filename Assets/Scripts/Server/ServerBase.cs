using Colyseus;
using Colyseus.Schema;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

public abstract class ServerBase<T> : MonoBehaviour where T : Schema
{
    public static ServerBase<T> instance;

    public static string sessionId;

    [Header("Events")]
    public UnityAction<Player> OnPlayerJoined;
    public UnityAction<Player> OnPlayerLeft;

    // Shared client instance - more efficient than creating one per server
    private static ColyseusClient sharedClient;
    protected ColyseusRoom<T> room;
    private bool hasJoined = false;

    protected string roomName;

    [SerializeField]
    private string debugSessionId;

    // WebGL: await 연속이 메인 스레드에서 안 돌 수 있어, Task 완료를 Update에서 폴링
    private Task<ColyseusRoom<T>> _pendingCreateTask;
    private Task<ColyseusRoom<T>> _pendingJoinByIdTask;

    protected virtual void Awake()
    {
        instance = this;

        // Create shared client if it doesn't exist
        if (sharedClient == null)
        {
            sharedClient = new ColyseusClient(Config.multiplayEngineServerUrl);
        }
    }


    // protected virtual void OnJoinRoom(ColyseusRoom<T> room)
    protected virtual void OnJoinRoom()
    {
        sessionId = room.SessionId;
        Debug.Log($"OnJoinRoom roomId: {room.RoomId}, sessionId: {sessionId}");
    }

    private void Update()
    {
        if (debugSessionId != sessionId)
            debugSessionId = sessionId;

        // WebGL: Task 완료를 메인 스레드에서 처리 (async 연속이 안 돌 수 있음)
        // OnJoinRoom()은 첫 state 수신 시 OnStateChanged(isFirstState)에서 호출함 (room.State 사용 가능한 시점)
        if (_pendingCreateTask != null)
        {
            if (_pendingCreateTask.IsCompleted)
            {
                var t = _pendingCreateTask;
                _pendingCreateTask = null;
                if (t.IsFaulted)
                {
                    Debug.LogError($"{GetType().Name}: CreateRoom failed: {t.Exception?.GetBaseException()?.Message}");
                    return;
                }
                room = t.Result;
                sessionId = room.SessionId;
                Debug.Log("Joined Room " + room.RoomId);
                RegisterRoomEvents(room);
            }
            return;
        }

        if (_pendingJoinByIdTask != null)
        {
            if (_pendingJoinByIdTask.IsCompleted)
            {
                var t = _pendingJoinByIdTask;
                _pendingJoinByIdTask = null;
                if (t.IsFaulted)
                {
                    Debug.LogError($"{GetType().Name}: JoinRoomById failed: {t.Exception?.GetBaseException()?.Message}");
                    return;
                }
                room = t.Result;
                sessionId = room.SessionId;
                Debug.Log("Joined Room " + room.RoomId);
                RegisterRoomEvents(room);
            }
        }
    }

    public void CreateRoom(Dictionary<string, object> options = null)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError($"{GetType().Name}: roomName is not set!");
            return;
        }

        if (room != null)
        {
            Debug.LogWarning($"{GetType().Name}: Already connected to room. Leave first before joining again.");
            return;
        }

        Debug.Log("CreateRoom: " + roomName);
        _pendingCreateTask = sharedClient.Create<T>(roomName, options);
    }

    public void JoinRoomById(string roomId, Dictionary<string, object> options = null)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError($"{GetType().Name}: roomId is not set!");
            return;
        }

        if (room != null)
        {
            Debug.LogWarning($"{GetType().Name}: Already connected to room. Leave first before joining again.");
            return;
        }

        _pendingJoinByIdTask = sharedClient.JoinById<T>(roomId, options);
    }

    protected virtual void RegisterRoomEvents(ColyseusRoom<T> room)
    {
        hasJoined = false;

        // Register OnStateChange to detect when room is fully initialized
        // The first state change indicates successful connection and state synchronization
        room.OnStateChange += OnStateChanged;
        
        // Register OnError event
        room.OnError += OnRoomError;
        
        // Register OnLeave event
        room.OnLeave += OnLeaveRoom;
        
        // Note: playerJoined/playerLeft 메시지는 각 구현체에서 필요시 등록
        // (예: LobbyServer에서만 사용, GameServer에서는 사용하지 않음)

        // 일부 환경/버전에서는 Join() Task 완료 시점에 첫 state를 이미 수신해
        // OnStateChange(isFirstState=true)를 놓칠 수 있음. 이 경우 State가 이미 있으면 즉시 Join 처리.
        if (!hasJoined && room != null && room.State != null)
        {
            hasJoined = true;
            StartCoroutine(CoOnJoinRoom());
        }
    }

    protected virtual void OnRoomError(int code, string message)
    {
        Debug.LogError($"{GetType().Name}: Room error - {message} (code: {code})");
    }

    /// <summary>
    /// OnJoinRoom()을 호출해도 안전한 시점인지 판단.
    /// 기본은 room/State/SessionId 준비 여부만 확인하고, 구현체에서 필요 필드를 추가로 체크할 수 있음.
    /// </summary>
    protected virtual bool IsRoomReadyForOnJoinRoom()
    {
        return room != null && room.State != null && !string.IsNullOrEmpty(room.SessionId);
    }

    private void OnStateChanged(T state, bool isFirstState)
    {
        Debug.Log($"[ServerBase] OnStateChanged isFirstState: {isFirstState}, hasJoined: {hasJoined}");
        // isFirstState=true를 놓친 경우를 대비해, 첫 콜백에서 state가 유효하면 join 처리
        if (!hasJoined && (isFirstState || state != null))
        {
            hasJoined = true;
            StartCoroutine(CoOnJoinRoom());
        }
    }

    // room / State / 세션 정보가 준비될 때까지 대기 (고정 지연 대신 조건 충족 시 즉시 진행)
    // Time.timeScale == 0 이어도 진행되도록 unscaledDeltaTime 기준 타임아웃
    private IEnumerator CoOnJoinRoom()
    {
        // yield return new WaitForSecondsRealtime(0.1f);
        const float timeoutSeconds = 15f;
        float elapsed = 0f;

        while (!IsRoomReadyForOnJoinRoom())
        {
            if (elapsed >= timeoutSeconds)
            {
                Debug.LogError($"{GetType().Name}: CoOnJoinRoom timed out after {timeoutSeconds}s (room not ready for OnJoinRoom).");
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        OnJoinRoom();
    }

    protected virtual void OnLeaveRoom(int code)
    {
        hasJoined = false;

        // Clean up event handlers
        if (room != null)
        {
            room.OnStateChange -= OnStateChanged;
            room.OnError -= OnRoomError;
            room.OnLeave -= OnLeaveRoom;
            room = null;
        }
    }

    public void LeaveRoom()
    {
        if (room != null)
        {
            Debug.Log("Leave Room " + room.RoomId);
            room.Leave();
        }
    }

    protected virtual void OnDestroy()
    {
        // Clean up when object is destroyed
        if (room != null)
        {
            room.OnStateChange -= OnStateChanged;
            room.OnError -= OnRoomError;
            room.OnLeave -= OnLeaveRoom;
            LeaveRoom();
        }
    }
}

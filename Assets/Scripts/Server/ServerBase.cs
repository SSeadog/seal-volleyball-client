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
    }

    protected virtual void OnRoomError(int code, string message)
    {
        Debug.LogError($"{GetType().Name}: Room error - {message} (code: {code})");
    }

    private void OnStateChanged(T state, bool isFirstState)
    {
        Debug.Log($"[ServerBase] OnStateChanged isFirstState: {isFirstState}, hasJoined: {hasJoined}");
        if (isFirstState && !hasJoined)
        {
            hasJoined = true;
            StartCoroutine(CoOnJoinRoom());
        }
    }

    // room 상태 동기화 위해 0.2초 대기
    // GameServer 등에서 Time.timeScale == 0 인 동안에도 대기가 끝나게 실시간 기준으로 대기
    private IEnumerator CoOnJoinRoom()
    {
        yield return new WaitForSecondsRealtime(0.2f);
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

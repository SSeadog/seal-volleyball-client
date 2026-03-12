using Colyseus;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class LobbyServer : ServerBase<LobbyRoomState>
{
    // ServerBase의 instance를 타입 안전하게 접근하는 property
    // new 키워드는 부모의 instance를 숨기고 property로 재정의한다는 의미
    public new static LobbyServer instance => ServerBase<LobbyRoomState>.instance as LobbyServer;

    /// <summary> 현재 룸의 방장 세션 ID (room state 기준) </summary>
    public static string RoomOwnerSessionId => instance?.room?.State?.roomOwnerSessionId ?? "";

    protected override void Awake()
    {
        base.Awake();

        roomName = "lobby_room";

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerJoined += OnPlayerJoinedHandler;
        
        OnPlayerLeft -= OnPlayerLeftHandler;
        OnPlayerLeft += OnPlayerLeftHandler;
    }

    // 메시지 핸들러를 저장하여 나중에 참조할 수 있도록 함
    private Action<Player> playerJoinedHandler;
    private Action<Player> playerLeftHandler;
    private Action<Dictionary<string, object>> matchEndHandler;

    protected override void RegisterRoomEvents(ColyseusRoom<LobbyRoomState> room)
    {
        base.RegisterRoomEvents(room);
        
        // LobbyServer에서만 사용하는 메시지 핸들러 등록
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

        // match_status 수신 시 매칭 타이머 표시 및 초기화
        room.OnMessage<Dictionary<string, object>>("match_status", OnMatchStatus);
        // match_end 메시지 핸들러 등록
        room.OnMessage<Dictionary<string, object>>("match_end", OnMatchEnd);
    }

    protected override void OnJoinRoom()
    {
        base.OnJoinRoom();

        // 룸 접속 성공 시 에러 핸들러 제거
        if (room != null)
        {
            room.OnError -= OnRoomError;
            
            GameServer.sessionId = room.SessionId;
            Debug.Log($"GameServer sessionId saved: {GameServer.sessionId}");

            // 룸 코드를 UI에 표시
            if (LobbyUIController.instance != null)
            {
                LobbyUIController.instance.SetRoomCode(room.RoomId);
                LobbyUIController.instance.RefreshBtnStartMatchingVisibility();
            }

            if (room.State.players != null)
            {
                for (int i = 0; i < room.State.players.Count; i++)
                {
                    LobbyUIController.instance.SetPlayerCard(room.State.players[i]);
                }
            }
        }
    }

    private void OnMatchStatus(Dictionary<string, object> message)
    {
        if (this == null || room == null) return;
        
        if (LobbyUIController.instance != null)
            LobbyUIController.instance.ShowAndResetMatchingTimer();
    }

    private void OnMatchEnd(Dictionary<string, object> message)
    {
        // 인스턴스가 파괴되었는지 확인
        if (this == null || room == null) return;
        
        // 서버에서 보낸 메시지에서 roomId와 roomName 추출
        if (message != null)
        {
            if (message.ContainsKey("roomId") && message["roomId"] != null)
            {
                GameServer.roomId = message["roomId"].ToString();
                GameServer.myPlayerIndex = int.Parse(message["playerIndex"].ToString());
                GameServer.myTeamIndex = int.Parse(message["teamIndex"].ToString());
                Debug.Log($"Received match_end message. RoomId: {GameServer.roomId} myPlayerIndex: {GameServer.myPlayerIndex}");
            }
        }
        
        Debug.Log("Moving to game scene...");
        SceneManager.LoadScene("VolleyBallScene");
    }

    protected override void OnRoomError(int code, string message)
    {
        base.OnRoomError(code, message);
        
        Debug.LogError($"Failed to join room: {message} (code: {code})");
        
        // 룸 접속 실패 시 새 룸 생성
        Debug.Log("Creating new room instead...");
        CreateRoom();
    }

    private void OnPlayerJoinedHandler(Player player)
    {
        LobbyUIController.instance.SetPlayerCard(player);
    }

    private void OnPlayerLeftHandler(Player player)
    {
        LobbyUIController.instance.RemovePlayerCard(player);
    }

    public async void SendStartMatchingMessage()
    {
        if (room != null)
        {
            await room.Send("start_matching");
            Debug.Log("Sent start_matching message to room");
        }
        else
        {
            Debug.LogWarning("Cannot send start_matching message: Not connected to room");
        }
    }

    protected override void OnLeaveRoom(int code)
    {
        // 룸을 null로 설정하여 메시지 핸들러가 호출되어도 안전하게 처리
        if (room != null)
        {
            room = null;
        }

        base.OnLeaveRoom(code);

        if (LobbyUIController.instance != null)
        {
            LobbyUIController.instance.ClearPlayerCards();
        }

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerLeft -= OnPlayerLeftHandler;
        
        // 핸들러 참조 제거
        playerJoinedHandler = null;
        playerLeftHandler = null;
        matchEndHandler = null;
    }

    protected override void OnDestroy()
    {
        // 룸을 먼저 null로 설정
        if (room != null)
        {
            room = null;
        }

        base.OnDestroy();

        OnPlayerJoined -= OnPlayerJoinedHandler;
        OnPlayerLeft -= OnPlayerLeftHandler;
        
        // 핸들러 참조 제거
        playerJoinedHandler = null;
        playerLeftHandler = null;
        matchEndHandler = null;
    }
}

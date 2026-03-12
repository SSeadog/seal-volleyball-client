// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 3.0.70
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

public partial class LobbyRoomState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public LobbyRoomState() { }
	[Type(0, "string")]
	public string phase = default(string);

	[Type(1, "array", typeof(ArraySchema<Player>))]
	public ArraySchema<Player> players = null;

	[Type(2, "number")]
	public float maxPlayers = default(float);

	[Type(3, "string")]
	public string roomId = default(string);

	[Type(4, "string")]
	public string roomOwnerSessionId = default(string);
}


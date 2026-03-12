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

public partial class Player : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public Player() { }
	[Type(0, "string")]
	public string sessionId = default(string);

	[Type(1, "string")]
	public string name = default(string);

	[Type(2, "number")]
	public float playerIndex = default(float);

	[Type(3, "number")]
	public float teamIndex = default(float);

	[Type(4, "boolean")]
	public bool isAI = default(bool);

	[Type(5, "boolean")]
	public bool isReady = default(bool);

	[Type(6, "boolean")]
	public bool isInMatchQueue = default(bool);

	[Type(7, "number")]
	public float posX = default(float);

	[Type(8, "number")]
	public float posY = default(float);

	[Type(9, "number")]
	public float offsetX = default(float);

	[Type(10, "number")]
	public float offsetY = default(float);

	[Type(11, "number")]
	public float sizeX = default(float);

	[Type(12, "number")]
	public float sizeY = default(float);
}


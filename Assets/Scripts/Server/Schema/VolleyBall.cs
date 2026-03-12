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

public partial class VolleyBall : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public VolleyBall() { }
	[Type(0, "number")]
	public float posX = default(float);

	[Type(1, "number")]
	public float posY = default(float);

	[Type(2, "number")]
	public float velX = default(float);

	[Type(3, "number")]
	public float velY = default(float);

	[Type(4, "number")]
	public float owningTeam = default(float);

	[Type(5, "number")]
	public float touchCount = default(float);
}


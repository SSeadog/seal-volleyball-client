using UnityEngine;

public class DebugLatencyCheck : MonoBehaviour
{
    private GUIStyle labelStyle;

    private void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24
            };
            labelStyle.normal.textColor = Color.white;
        }

        // DebugFrameRate에서 (10,10)에 FPS를 그리고 있으므로,
        // 그 바로 아래쪽에 좌표를 그린다.
        GUI.Label(
            new Rect(10, 70, 600, 40),
            $"Latency: {GameServer.LatencyMs:F2} ms",
            labelStyle
        );
    }
}


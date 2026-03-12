using UnityEngine;

public class DebugFrameRate : MonoBehaviour
{
    // FPS 계산용 (지수 이동 평균 방식)
    private float deltaTime = 0.0f;

    private void Update()
    {
        // Time.unscaledDeltaTime을 사용해 timeScale의 영향을 받지 않도록 함
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float fps = 1.0f / deltaTime;
        int target = Application.targetFrameRate;
        string targetText = target > 0 ? target.ToString() : "unlimited";

        // 왼쪽 상단에 FPS / Target 표시
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(10, 10, 400, 30), $"FPS: {fps:F1}  Target: {targetText}", style);
    }
}

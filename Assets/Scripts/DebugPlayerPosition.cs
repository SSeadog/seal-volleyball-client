using UnityEngine;

public class DebugPlayerPosition : MonoBehaviour
{
    private PlayerView localPlayerView;
    private GUIStyle labelStyle;

    private void Update()
    {
        // 아직 로컬 플레이어를 찾지 못했으면 매 프레임 한 번씩 시도
        if (localPlayerView == null)
        {
            var views = FindObjectsOfType<PlayerView>();
            foreach (var view in views)
            {
                if (view != null && view.IsMine)
                {
                    localPlayerView = view;
                    break;
                }
            }
        }
    }

    private void OnGUI()
    {
        if (localPlayerView == null)
        {
            return;
        }

        Vector3 pos = localPlayerView.transform.position;

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
            new Rect(10, 40, 600, 40),
            $"Player Pos  x: {pos.x:F2}  y: {pos.y:F2}",
            labelStyle
        );
    }
}


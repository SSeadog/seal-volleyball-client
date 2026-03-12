using UnityEngine;

public class VolleyBallView : MonoBehaviour
{
    public static VolleyBallView instance;

    [SerializeField] private float interpolationSpeed = 10f;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        // GameServer에서 내려주는 서버 볼 위치를 기반으로 보간
        if (GameServer.instance == null || !GameServer.HasBallServerPosition) return;

        float serverPosX = GameServer.ServerBallPosX;
        float serverPosY = GameServer.ServerBallPosY;

        Vector3 clientPosition = transform.position;
        Vector3 targetPosition = new Vector3(serverPosX, serverPosY, clientPosition.z);

        transform.position = Vector3.Lerp(clientPosition, targetPosition, Time.deltaTime * interpolationSpeed);
    }
}



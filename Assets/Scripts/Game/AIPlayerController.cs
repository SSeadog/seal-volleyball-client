using System.Collections;
using UnityEngine;

public class AIPlayerController : MonoBehaviour
{
    // 공 궤도 예측
    // 공 착지 지점으로 이동
    // 역할 분담(리시브/디그, 토스, 스파이크)

    enum EState
    {
        Receive,
        Dig,
        Toss,
        Spike
    }

    [SerializeField] private bool tryFirstTouch;
    [SerializeField] private Transform hand;
    [Header("Trajectory Debug")]
    [SerializeField] private Transform net;                      // 네트 Transform (충돌 예측용)

    private PlayerController playerController;

    private Vector3 ballNearHandPos;

    private float testCoolTime = 0.3f;
    private float coolTimer;

    private Vector3 initPos;

    private VolleyBallController volleyBall;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        initPos = transform.position;
        coolTimer = testCoolTime;

        volleyBall = VolleyBallScene.Instance.VolleyBallController;
    }

    void Update()
    {
        if (coolTimer > 0f)
        {
            // Debug.Log("쿨타임중");
            coolTimer -= Time.deltaTime;
            return;
        }

        ballNearHandPos = volleyBall.GetNearestPos(hand.position);
        // ballNearHandPos = volleyBall.HitGroundPos;

        // 자신이 마지막 터치면, 공쪽으로 천천히 이동
        if (VolleyBallGameManager.instance.GetLastTouchPlayerId() == playerController.GetId())
        {
            Vector3 normal = (ballNearHandPos - hand.position).normalized;

            float moveX = normal.x * 0.7f;
            playerController.Move(moveX);

            return;
        }

        // 블루팀인데 공이 레드팀 쪽에 있거나 레드팀인데 공이 블루팀 쪽에 있다면
        if ((playerController.IsBlueTeam() == true&& VolleyBallGameManager.instance.IsBallInBlueSide() == false)
            || playerController.IsBlueTeam() == false && VolleyBallGameManager.instance.IsBallInBlueSide() == true)
        {
            // initPos로 이동
            float moveX = (initPos - transform.position).normalized.x * 0.3f;
            playerController.Move(moveX);

            // 다른 행동은 하지 않기
            return;
        }

        if (tryFirstTouch == true && VolleyBallGameManager.instance.GetTouchCount() == 0)
        {
            UpdateReceive();
        }
        else if (VolleyBallGameManager.instance.GetTouchCount() == 1)
        {
            UpdateToss();
        }
        else if (VolleyBallGameManager.instance.GetTouchCount() == 2)
        {
            UpdateSpike();
        }
    }

    void UpdateReceive()
    {
        // 공 착지 지점에서 거리가 있다면 공 착지 지점으로 이동
        if (ballNearHandPos != Vector3.zero && (ballNearHandPos - hand.position).magnitude > 0.75f)
        {
            Vector3 normal = (ballNearHandPos - hand.position).normalized;

            float moveX = normal.x * 0.9f;
            playerController.Move(moveX);
        }
        else
        {
            float dist = (volleyBall.transform.position - hand.position).magnitude;
            if (dist < 0.75f)
            {
                // Debug.Log("리시브 시도");
                playerController.Receive();

                coolTimer = testCoolTime;
            }
        }
    }

    void UpdateToss()
    {
        // 공 착지 지점에서 거리가 있다면 공 착지 지점으로 이동
        if (ballNearHandPos != Vector3.zero && (ballNearHandPos - hand.position).magnitude > 0.75f)
        {
            Vector3 normal = (ballNearHandPos - hand.position).normalized;

            float moveX = normal.x * 0.6f;
            playerController.Move(moveX);
        }
        else
        {
            {
                int segments = 24;
                float step = Mathf.PI * 2f / segments;
                Vector3 center = hand.position;
                Vector3 prev = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * PhysicsConstants.PLAYER_HAND_CHECK_RADIUS;
                for (int i = 1; i <= segments; i++)
                {
                    float a = step * i;
                    Vector3 next = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * PhysicsConstants.PLAYER_HAND_CHECK_RADIUS;
                    Debug.DrawLine(prev, next, Color.red, 1f);
                    prev = next;
                }
            }

            // 공과의 거리가 가까워 토스할 수 있으면 토스 시도
            float dist = (volleyBall.transform.position - hand.position).magnitude;
            if (dist < 0.5f)
            {
                // Debug.Log("토스 시도");
                playerController.Toss();

                coolTimer = testCoolTime;
            }
        }
    }

    // 점프해서 고점까지 도달 시간 = 0.3초로 계산
    // x축 남은 거리와 y축 남은 거리 각자 계산
    // x축 남은 거리가 충분히 짧고 y축 남은 거리가 최대 점프 높이보다 낮다면(0.3초 이후) 점프하여 스파이크 시도
    void UpdateSpike()
    {
        // 공중 타격 y위치 기준 공 위치까지 이동
        Vector3 ballPosInFuture = volleyBall.PredictBallPos(0.73f);
        Vector3 targetPos = ballPosInFuture;
        
        // 공 위치까지의 거리 체크
        float distToBall = (targetPos - hand.position).magnitude;
        float distX = (targetPos.x - hand.position.x);
        float distY = (targetPos.y - 4.38f - 0.4f); // 점프했을 때 타격 지점 기준 y + 살짝 높게(0.4f) 그래야 멀리 날림
        
        // 일단 이대로 쓰고 다듬자
        // if (Mathf.Abs(distX) > 0.3f || Mathf.Abs(distY) > 0.3f ) // 공이 멀면 이동
        if (Mathf.Abs(distY) > 0.1f ) // y축으로 공이 멀면 계속 이동.
        {
            Vector3 direction = (targetPos - hand.position).normalized;
            float moveX = direction.x * 1f; // 이동 속도
            playerController.Move(moveX);
        }
        else // 공이 가까우면 제자리 점프해서 스파이크 시도
        {
            // 스파이크 시도할 때 예측 공 높이 debug 해보자
            {
                int segments = 24;
                float step = Mathf.PI * 2f / segments;
                Vector3 center = hand.position;
                Vector3 prev = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * PhysicsConstants.PLAYER_HAND_CHECK_RADIUS;
                for (int i = 1; i <= segments; i++)
                {
                    float a = step * i;
                    Vector3 next = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * PhysicsConstants.PLAYER_HAND_CHECK_RADIUS;
                    Debug.DrawLine(prev, next, Color.red, 1f);
                    prev = next;
                }
            }

            // 제자리 점프
            playerController.Jump();
            
            // 점프 후 스파이크 시도 (약간의 딜레이 후)
            StartCoroutine(CoSpikeAfterJump());
            
            coolTimer = testCoolTime;
        }
    }
    
    IEnumerator CoSpikeAfterJump()
    {
        // 점프하여 최고 높이에 도달할 때까지 대기(점프 파워를 변경하지 않으면 이 값은 바뀌지 않을 듯)
        // 예측 시간 보다는 짧아야 할듯. 공 속도 고려를 안해서(공이 올라갈 수도 내려갈 수도 있으나, 내려오는 경우만 성공하는 것으로 간주)
        // 추후 내려오는 공에만. 가능하면 올라가는 공도 시도해보도록 개발 필요
        yield return new WaitForSeconds(0.73f);

        // 점프한 후에도 x축 거리가 멀다면 계속 이동
        float moveTime = 1f;
        while (moveTime > 0f)
        {
            moveTime -= Time.deltaTime;
            // 공중 타격 y위치 기준 공 위치까지 이동
            Vector3 ballPosInFuture = volleyBall.PredictBallPos(moveTime);
            Vector3 targetPos = ballPosInFuture;
            
            // 공 위치까지의 거리 체크
            float distX = (targetPos.x - hand.position.x);

            // 공이 멀면 이동
            if (Mathf.Abs(distX) > 0.1f)
            {
                Vector3 direction = (targetPos - hand.position).normalized;
                float moveX = direction.x * 1f; // 이동 속도
                playerController.Move(moveX);
            }

            // 스파이크 시도
            float dist = (volleyBall.transform.position - hand.position).magnitude;
            Debug.Log("스파이크 시도!! 공과의 거리: " + dist + ", hand y: " + hand.position.y);
            
            if (dist <= 0.75f) // 공이 여전히 가까우면
            {
                Debug.Log("스파이크 시도");
                playerController.Spike();
                coolTimer = testCoolTime;
                yield break;
            }

            yield return null;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

// 씬에서 들고 있고, 씬 통해서 찾아서 사용
public class VolleyBallController : MonoBehaviour
{
    [SerializeField] private Vector3 initPosition;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform net;

    [Header("Trajectory Debug")]
    [SerializeField] private int trajectorySteps = 120;          // 그릴 샘플 수
    [SerializeField] private float trajectoryTimeStep = 0.033f;  // 샘플 간 시간 간격 (약 30Hz)
    [SerializeField] private Color trajectoryColor = Color.cyan; // 라인 색상
    [SerializeField] private LayerMask groundMask;               // 착지 판정용 레이어(선택)
    [SerializeField] private float netHalfWidth = 0.15f;         // 네트 x축 반폭(추정)
    [SerializeField] private float netMinY = 0f;                 // 네트 하단 y(추정)
    [SerializeField] private float netMaxY = 3.6f;               // 네트 상단 y(추정)

    private bool isInBlueSide;

    private Rigidbody2D rb2d;
    
    private Dictionary<float, Vector3> predictPosDict;          // 공 위치 예측 dict<시간, 위치>
    private Vector3 hitGroundPos;
    public Vector3 HitGroundPos { get { return hitGroundPos; } }

    void Start()
    {
        isInBlueSide = true;
        // rb2d = GetComponent<Rigidbody2D>();

        VolleyBallGameManager.instance.OnBallInited += InitPosition;
        predictPosDict = new Dictionary<float, Vector3>();
    }

    void Update()
    {
        if (VolleyBallGameManager.instance.IsGameEnd() == true)
            return;

        isInBlueSide = (net.position.x - transform.position.x) > 0f;
        Debug.Log("Ball isInBlueSide: " + isInBlueSide);

        PredictBallTrajectory();
    }

    void FixedUpdate()
    {
        if (VolleyBallGameManager.instance.IsGameEnd() == true)
            return;

        if (IsGrounded())
        {
            // Debug.Log("Grounded. InitPosition");
            // VolleyBallGameManager.instance.InitBall();
            // VolleyBallGameManager.instance.RaiseScore(isBlueTeam: isInBlueSide == false);
        }

        if (IsBallGoOverNet())
        {
            VolleyBallGameManager.instance.ClearTouchCount();
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            // groundCheck 미지정 시, 발 아래로 간단한 원형 체크를 현재 위치 기준으로 수행
            return Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer) != null;
        }
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }

    private bool IsBallGoOverNet()
    {
        float netXPos = net.position.x;
        float netHeight = 3.6f;
        // 공 y값이 네트 높이보다 높고 x축 기준 충분히 가깝다면
        if (transform.position.y > netHeight && Mathf.Abs(netXPos - transform.position.x) < 0.5f)
        {
            return true;
        }

        return false;
    }

    private void InitPosition()
    {
        transform.position = initPosition;
        // rb2d.linearVelocity = Vector2.zero;
    }

    // 예측 로직인데 예측만 하는 게 아니라, 실제 충돌 계산도 함
    // 이 부분은 분리가 되어야 할듯
    private void PredictBallTrajectory()
    {
        // 예측 전에 기존 값 clear
        predictPosDict.Clear();

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // 시뮬레이션 초기 상태: 현재 위치/속도
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        // 실제 물리와 동일하게 중력 적용 (GravityScale 반영)
        Vector2 gravity = Physics2D.gravity * rb.gravityScale;

        Vector3[] trajectoryPosArr = new Vector3[trajectorySteps];

        // 샘플 간 선분으로 궤적 그리기
        for (int i = 0; i < trajectorySteps; i++)
        {
            // 다음 프레임의 속도/위치(등가속도 운동)
            Vector2 nextVel = vel + gravity * trajectoryTimeStep;
            Vector2 nextPos = pos + vel * trajectoryTimeStep + 0.5f * gravity * trajectoryTimeStep * trajectoryTimeStep;

            Debug.DrawLine(pos, nextPos, trajectoryColor);

            // 네트와의 교차 시 속도 변경(TestVolleyBall 규칙 적용)
            if (net != null)
            {
                float nx = net.position.x;
                // bool crosses = (pos.x - nx) * (nextPos.x - nx) <= 0f; // 선분이 x=nx를 가로지름
                bool crosses = Mathf.Abs(nextPos.x - nx) < 0.3f; // x축 기준 거리가 공의 반지름보다 작은지
                // Debug.Log("공이 네트에 닿냐? " + crosses);
                if (crosses)
                {
                    // 선형보간으로 교차 지점 계산
                    float denom = (nextPos.x - pos.x);
                    float t = Mathf.Approximately(denom, 0f) ? 0f : (nx - pos.x) / denom;
                    t = Mathf.Clamp01(t);
                    Vector2 hitPoint = pos + (nextPos - pos) * t;
                    if (hitPoint.y >= netMinY && hitPoint.y <= netMaxY - 0.25f)
                    {
                        // 속도 줄여서 공 반대 방향으로 속도 전환

                        // 충돌 처리: 속도 변경 후 남은 구간 계속 시뮬레이션
                        Debug.DrawLine(pos, hitPoint, Color.magenta);
                        // TestVolleyBall: v = (v.x * -0.3f, v.y * 0.5f)
                        vel = new Vector2(vel.x * -0.5f, vel.y * 0.5f);
                        // 남은 시간 비율
                        Vector2 nextVelAfter = vel + gravity * trajectoryTimeStep;
                        Vector2 nextPosAfter = hitPoint + vel * trajectoryTimeStep + 0.5f * gravity * trajectoryTimeStep * trajectoryTimeStep;
                        Debug.DrawLine(hitPoint, nextPosAfter, trajectoryColor);
                        pos = nextPosAfter;
                        vel = nextVelAfter;
                        continue; // 다음 스텝으로
                    }
                    else if(hitPoint.y >= netMaxY - 0.25f && hitPoint.y <= netMaxY + 0.25f)
                    {
                        // 속도 줄여서 공 네트 넘기기
                        Debug.DrawLine(pos, hitPoint, Color.magenta);

                        vel = new Vector2(vel.x * 0.5f, vel.y * -0.5f);
                        // 남은 시간 비율
                        Vector2 nextVelAfter = vel + gravity * trajectoryTimeStep;
                        Vector2 nextPosAfter = hitPoint + vel * trajectoryTimeStep + 0.5f * gravity * trajectoryTimeStep * trajectoryTimeStep;
                        Debug.DrawLine(hitPoint, nextPosAfter, trajectoryColor);
                        pos = nextPosAfter;
                        vel = nextVelAfter;
                        continue; // 다음 스텝으로
                    }
                }
            }

            trajectoryPosArr[i] = pos;
            // Debug.Log("trajectoryTimeStep * i: " + trajectoryTimeStep * i);
            predictPosDict.Add(trajectoryTimeStep * i, pos);

            // 지면/충돌 레이어와의 교차 시 조기 종료 (선택)
            if (groundMask.value != 0)
            {
                RaycastHit2D hit = Physics2D.Linecast(pos, nextPos, groundMask);
                if (hit.collider != null)
                {
                    // 착지 지점까지 라인만 그리고 중단
                    Debug.DrawLine(pos, hit.point, Color.yellow);
                    hitGroundPos = pos;
                    break;
                }
            }

            // 다음 스텝으로 진행
            pos = nextPos;
            vel = nextVel;
        }
    }

    // time초 이후 공 위치 예측
    public Vector3 PredictBallPos(float time)
    {
        if (predictPosDict.Count == 0)
            return Vector3.zero;

        float nearestKey = 99999f; // 충분히 먼 값 설정
        foreach (float key in predictPosDict.Keys)
        {
            if (Mathf.Abs(time - key) < Mathf.Abs(time - nearestKey))
                nearestKey = key;
        }

        // 터무니 없이 먼 시간이면 제외
        if (Mathf.Abs(time - nearestKey) >= 0.033f)
            return Vector3.zero;

        return predictPosDict[nearestKey];
    }

    public Vector3 GetNearestPos(Vector3 pos)
    {
        Vector3 nearestPos = Vector3.one * 9999f; // 충분히 먼 값

        foreach (float key in predictPosDict.Keys)
        {
            if ((pos - predictPosDict[key]).magnitude < (pos - nearestPos).magnitude)
            {
                nearestPos = predictPosDict[key];
            }
        }

        // 초기 값(충분히 먼 값)이 선택되는 경우는 예외 처리해야할 듯
        return nearestPos;
    }

	private void OnDrawGizmos()
	{
		// 씬 뷰에서 Ground Check 범위를 시각화
		bool groundedPreview = false;
		if (Application.isPlaying)
		{
			groundedPreview = IsGrounded();
		}

		Gizmos.color = groundedPreview ? Color.green : Color.red;
		Vector3 center = groundCheck != null ? groundCheck.position : transform.position;
		Gizmos.DrawWireSphere(center, groundCheckRadius);
	}

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Debug.Log("공이 부딪힌 트리거: " + collider.gameObject.name);
        if (collider.gameObject.name == "Net")
        {
            // Vector2 v = rb2d.linearVelocity;
            // v = new Vector2(v.x * -0.3f, v.y * 0.5f);
            // rb2d.linearVelocity = v;
        }
    }
}

using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private string id; // 세션id로 대체..?
    [SerializeField] private bool isLeftTeam;

    [Header("Net")]
    [SerializeField] private Transform netTransform;

    [Header("UI")]
    [SerializeField] private GameObject myPlayerImage;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float playerSize = 3.65f; // 플레이어 몸 크기 (반지름 또는 너비)

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Hand")] 
    [SerializeField] private Transform hand;
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private float force = 5f;
    [SerializeField] private float tossForce;

	[Header("Receive Tuning")]
	[SerializeField] private float receiveXDecay = 0.3f; // x속도 감쇠 비율
	[SerializeField] private float receiveXStopThreshold = 2f; // 이하면 0으로 고정
	[SerializeField] private float receivePopUpForce = 6f; // 위로 튀기는 힘

	[Header("Dig Slide")]
	[SerializeField] private float digSlideForce = 8f;
	[SerializeField] private float digSlideDuration = 0.4f;
	[SerializeField] private float digRotateAngle = 90f;
	[SerializeField] private float digRecoverDuration = 0.15f;

	[Header("Hand Action Cooldown")]
	[SerializeField] private float handActionCooldown = 0.1f;

    private Rigidbody2D rb2d;    

    private Animator animator;

	private bool isDigging;
	private Quaternion defaultRotation;

    // 서버와 동일한 커스텀 물리 변수
    private float velX = 0f;
    private float velY = 0f;

    private float lastHandActionTime = -999f;

    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        
        // Rigidbody2D는 충돌 감지용으로만 사용, 물리 시뮬레이션은 직접 처리
        if (rb2d != null)
        {
            rb2d.bodyType = RigidbodyType2D.Kinematic;
        }

        // 기본값은 비활성화, 로컬 플레이어 초기화 시에만 활성화
        if (myPlayerImage != null)
        {
            myPlayerImage.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        
        // 중력 적용 (서버와 동일: velY += GRAVITY * deltaTime)
        velY += PhysicsConstants.GRAVITY * dt;
        
        // 위치 업데이트
        Vector3 pos = transform.position;
        pos.x += velX * dt;
        pos.y += velY * dt;
        
        // 땅 충돌 체크 (서버와 동일: playerBottom <= groundTop)
        float groundTop = PhysicsConstants.PLAYER_GROUND_Y + 0.5f; // 땅 상단 = 0
        float playerBottom = pos.y - PhysicsConstants.PLAYER_SIZE_Y / 2f;
        
        if (playerBottom <= groundTop)
        {
            pos.y = groundTop + PhysicsConstants.PLAYER_SIZE_Y / 2f;
            velY = 0f;
        }
        
        // 네트 충돌 제한
        if (netTransform != null)
        {
            float netX = netTransform.position.x;
            float playerHalfSize = playerSize * 0.5f;
            
            if (isLeftTeam && pos.x + playerHalfSize > netX)
            {
                pos.x = netX - playerHalfSize;
                velX = 0f;
            }
            else if (!isLeftTeam && pos.x - playerHalfSize < netX)
            {
                pos.x = netX + playerHalfSize;
                velX = 0f;
            }
        }
        
        transform.position = pos;
    }

    public void Init(Transform netTransform, bool isLeftTeam = true, bool isMyPlayer = false)
    {
        this.netTransform = netTransform;
        this.isLeftTeam = isLeftTeam;

        if (isLeftTeam == false)
        {
            GetComponentInChildren<SpriteRenderer>().flipX = true;
        }

        // 로컬 플레이어라면 표시용 이미지 활성화
        if (isMyPlayer && myPlayerImage != null)
        {
            myPlayerImage.SetActive(true);
        }
    }

    public string GetId()
    {
        return id;
    }

    public bool IsBlueTeam()
    {
        return isLeftTeam;
    }


    public void Move(float moveX)
    {
        // 서버와 동일한 이동 처리: 입력이 있으면 고정 속도, 없으면 감쇠
        if (Mathf.Abs(moveX) > 0.1f)
        {
            velX = moveX > 0 ? PhysicsConstants.PLAYER_MOVE_SPEED : -PhysicsConstants.PLAYER_MOVE_SPEED;
        }
        else
        {
            // 좌우 입력이 없으면 속도 감쇠
            velX *= PhysicsConstants.PLAYER_VELOCITY_DECAY;
            if (Mathf.Abs(velX) < PhysicsConstants.VELOCITY_THRESHOLD)
            {
                velX = 0f;
            }
        }
    }

    public bool IsDigging()
    {
        return isDigging;
    }

    /// <summary> 서버 메시지 수신 시 애니메이션만 재생 (리시브/디그) </summary>
    public void PlayReceiveAnimation()
    {
        if (animator != null) animator.SetTrigger("Dig");
    }

    /// <summary> 서버 메시지 수신 시 애니메이션만 재생 (토스) </summary>
    public void PlayTossAnimation()
    {
        if (animator != null) animator.SetTrigger("Toss");
    }

    /// <summary> 서버 메시지 수신 시 애니메이션만 재생 (스파이크) </summary>
    public void PlaySpikeAnimation()
    {
        if (animator != null) animator.SetTrigger("Spike");
    }

    /// <summary> 서버 메시지 수신 시 애니메이션만 재생 (점프) </summary>
    public void PlayJumpAnimation()
    {
        if (animator != null) animator.SetTrigger("Jump");
    }

    /// <summary>
    /// 점프 처리 (서버와 동일: 땅에 있고 velY <= 0일 때만 점프)
    /// </summary>
    public void Jump()
    {
        float groundTop = PhysicsConstants.PLAYER_GROUND_Y + 0.5f;
        float playerBottom = transform.position.y - PhysicsConstants.PLAYER_SIZE_Y / 2f;
        bool isOnGround = Mathf.Abs(playerBottom - groundTop) < 0.1f;
        
        if (isOnGround && velY <= 0f)
        {
            velY = PhysicsConstants.PLAYER_JUMP_VELOCITY;
            animator.SetTrigger("Jump");
        }
    }

    public void Receive(float moveX = 0f)
    {
        if (Time.time - lastHandActionTime < handActionCooldown) return;
        lastHandActionTime = Time.time;
        animator.SetTrigger("Dig");

        // 로컬 플레이어 리시브 효과음
        SoundManager.Instance.PlaySfx("Seal_Dig");
        
        bool left = moveX < -0.1f;
        bool right = moveX > 0.1f;
        SendInputToServer(left, right, false, true, false, false);
    }

    // public void Dig(float moveX)
    // {
    //     animator.SetTrigger("Dig");

    //     Collider2D ball = Physics2D.OverlapCircle(hand.position, PhysicsConstants.PLAYER_HAND_CHECK_RADIUS, ballLayer);
    //     if (ball == null) return;

    //     Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();

    //     Vector2 direction = (hand.position - ballRb.transform.position).normalized;
    //     ballRb.AddForce(direction * force, ForceMode2D.Impulse);

    //     VolleyBallGameManager.instance.CountTouch();
    //     VolleyBallGameManager.instance.SetLastTouchPlayerId(id);
    // }

    // 토스
    // 네트 기준 Vector3(-1, 8, 0) 위치에 포물선으로 공을 던지기
    // 최대 힘을 두고, 해당 힘 이하로만 공 띄울 수 있게 하기
    // private void Toss(InputAction.CallbackContext ctx)
    public void Toss(float moveX = 0f)
    {
        if (Time.time - lastHandActionTime < handActionCooldown) return;
        lastHandActionTime = Time.time;
        animator.SetTrigger("Toss");

        // 로컬 플레이어 토스 효과음
        SoundManager.Instance.PlaySfx("Seal_Toss");

        bool left = moveX < -0.1f;
        bool right = moveX > 0.1f;
        SendInputToServer(left, right, false, false, true, false);
    }
    
    /// <summary>
    /// 포물선 운동을 위한 초기 속도를 계산합니다.
    /// </summary>
    /// <param name="startPos">시작 위치</param>
    /// <param name="targetPos">목표 위치</param>
    /// <param name="maxForce">최대 힘 (속도 제한)</param>
    /// <returns>계산된 초기 속도</returns>
    private Vector2 CalculateParabolicVelocity(Vector3 startPos, Vector3 targetPos, float maxForce)
    {
        // 중력 가속도 (Unity 2D에서 양수로 사용)
        float gravity = Mathf.Abs(Physics2D.gravity.y);
        if (gravity == 0) gravity = 9.81f; // 기본값 설정
        
        // 수평 및 수직 거리
        float deltaX = targetPos.x - startPos.x;
        float deltaY = targetPos.y - startPos.y;
        
        // 방향
        float theta = Mathf.Atan2(deltaY, deltaX);

        // Debug.Log("직선 방향 theta: " + theta);
        
        // 힘
        // float power = (targetPos - startPos).magnitude; // 거리
        
        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * tossForce;
    }

    // 강공격
    // private void Spike(InputAction.CallbackContext ctx)
    public void Spike(float moveX = 0f)
    {
        if (Time.time - lastHandActionTime < handActionCooldown) return;
        lastHandActionTime = Time.time;
        animator.SetTrigger("Spike");

        // 로컬 플레이어 스파이크 효과음
        SoundManager.Instance.PlaySfx("Seal_Spike");

        bool left = moveX < -0.1f;
        bool right = moveX > 0.1f;
        SendInputToServer(left, right, false, false, false, true);
    }

    /// <summary>
    /// 입력을 서버로 전송합니다. (move/jump는 핸들러에서, receive/toss/spike는 본 메서드 내부에서 호출)
    /// </summary>
    public void SendInputToServer(bool left, bool right, bool jump, bool receive, bool toss, bool spike)
    {
        if (GameServer.instance == null) return;
        string currentSessionId = GameServer.sessionId;
        if (string.IsNullOrEmpty(currentSessionId)) return;

        var inputData = new PlayerInputData
        {
            tick = Time.frameCount,
            sessionId = currentSessionId,
            left = left,
            right = right,
            jump = jump,
            receive = receive,
            toss = toss,
            spike = spike
        };
        GameServer.instance.SendInputMessage(inputData);
    }

	// private IEnumerator DigSlide(float moveX)
	// {
	// 	if (isDigging) yield break;
	// 	isDigging = true;

    //     Debug.Log("Dig Slide Start");

	// 	// 눕히기: 이동 방향에 따라 z축 ±90도 회전
	// 	float signedAngle = (moveX >= 0f ? -1f : 1f) * digRotateAngle;
	// 	transform.rotation = Quaternion.Euler(0f, 0f, signedAngle);

	// 	// 슬라이딩 임펄스 적용 (커스텀 물리: 직접 velX에 적용)
	// 	velX = Mathf.Sign(moveX) * digSlideForce;

	// 	float t = 0f;
	// 	while (t < digSlideDuration)
	// 	{
	// 		t += Time.deltaTime;
	// 		yield return null;
	// 	}

	// 	// 원래 회전값으로 복귀 (짧게 보간)
	// 	Quaternion startRot = transform.rotation;
	// 	t = 0f;
	// 	while (t < digRecoverDuration)
	// 	{
	// 		t += Time.deltaTime;
	// 		float lerp = Mathf.Clamp01(t / digRecoverDuration);
	// 		transform.rotation = Quaternion.Slerp(startRot, defaultRotation, lerp);
	// 		yield return null;
	// 	}
	// 	transform.rotation = defaultRotation;

	// 	isDigging = false;
	// }

    /// <summary>
    /// 서버와 동일한 그라운드 체크
    /// </summary>
    private bool IsGrounded()
    {
        float groundTop = PhysicsConstants.PLAYER_GROUND_Y + 0.5f;
        float playerBottom = transform.position.y - PhysicsConstants.PLAYER_SIZE_Y / 2f;
        return Mathf.Abs(playerBottom - groundTop) < 0.1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = groundCheck != null ? groundCheck.position : transform.position;
        Gizmos.DrawWireSphere(center, groundCheckRadius);
    }
}

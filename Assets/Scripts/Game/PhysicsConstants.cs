/// <summary>
/// 게임 물리 상수 정의
/// </summary>
public static class PhysicsConstants
{
    // 중력 및 시간
    public const float GRAVITY = -9.8f; // 중력 가속도 (m/s²)
    public const float DELTA_TIME = 16f; // 60 FPS 기준 deltaTime (ms)

    // 볼 관련
    public const float BALL_RADIUS = 0.3f; // 볼의 반지름
    public const float TOSS_VELOCITY = 10.0f; // 공을 위로 띄우는 속도
    public const float TOSS_DISTANCE_THRESHOLD = 0.5f; // 공을 띄울 수 있는 최대 거리

    // 충돌 및 물리
    public const float BOUNCE_FACTOR = 0.7f; // 탄성 계수 (0.7 = 70% 에너지 보존)
    public const float FRICTION = 0.95f; // 마찰 계수

    // 속도 임계값 (이보다 작으면 정지)
    public const float VELOCITY_THRESHOLD = 0.1f;

    // 플레이어 관련
    public const float PLAYER_JUMP_VELOCITY = 7.66f; // 2m 점프를 위한 초기 속도 (√(2 * 9.8 * 3))
    public const float PLAYER_MOVE_SPEED = 4.0f; // 플레이어 이동 속도 (m/s)
    public const float PLAYER_VELOCITY_DECAY = 0.75f; // 좌우 입력 없을 때 수평 속도 감쇠 비율
    public const float PLAYER_GROUND_Y = -0.5f; // 땅의 Y 좌표 (땅 중심이 -0.5, 높이 1이므로 상단은 0)
    public const float PLAYER_SIZE_Y = 1.8f; // 플레이어 높이
    public const float PLAYER_HAND_CHECK_RADIUS = 0.95f; // 손 체크 반경 (공 상호작용 범위, Toss/Receive/Spike 공통)
}

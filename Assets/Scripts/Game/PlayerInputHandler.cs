using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Actions (Input System)")]
    [Tooltip("Input System의 Move 액션을 할당하세요 (Vector2 권장). x축만 사용합니다.")]
    [SerializeField] private InputActionReference moveActionRef;
    
    [Tooltip("Input System의 Jump 액션을 할당하세요.")]
    [SerializeField] private InputActionReference jumpActionRef;

    [Tooltip("Input System의 리시브/디그 액션을 할당하세요.")]
    [SerializeField] private InputActionReference handActionRef;

    [Tooltip("Input System의 토스 액션을 할당하세요.")]
    [SerializeField] private InputActionReference handActionRef2;

    [Tooltip("Input System의 스파이크 액션을 할당하세요.")]
    [SerializeField] private InputActionReference attackActionRef;

    private InputAction moveAction;
    private InputAction jumpAction;
    // 리시브, 디그
    private InputAction handAction;
    // 토스
    private InputAction handAction2;
    // 스파이크
    private InputAction attackAction;

    private PlayerController playerController;

    private float cachedMoveX = 0f;

    private void Awake()
    {
		if (moveActionRef != null) moveAction = moveActionRef.action;
        if (jumpActionRef != null) jumpAction = jumpActionRef.action;
        if (handActionRef != null) handAction = handActionRef.action;
        if (handActionRef2 != null) handAction2 = handActionRef2.action;
        if (attackActionRef != null) attackAction = attackActionRef.action;

        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
            moveAction.Enable();
        }
        if (jumpAction != null)
        {
            jumpAction.performed += OnJumpPerformed;
            jumpAction.Enable();
        }
        if (handAction != null)
        {
            handAction.performed += OnHandPerformed;
            handAction.Enable();
        }
        if (handAction2 != null)
        {
            handAction2.performed += OnHandPerformed2;
            handAction2.Enable();
        }
        if (attackAction != null)
        {
            attackAction.performed += OnAttackPerformed;
            attackAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Disable();
        }
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }
        if (handAction != null)
        {
            handAction.performed -= OnHandPerformed;
            handAction.Disable();
        }
        if (handAction2 != null)
        {
            handAction2.performed -= OnHandPerformed2;
            handAction2.Disable();
        }
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPerformed;
            attackAction.Disable();
        }
    }


    private void FixedUpdate()
    {
		// 좌우 이동 (디그 슬라이딩 중엔 x속도를 덮어쓰지 않음)
        // Debug.Log("playerController.IsDigging(): " + playerController.IsDigging());
		if (!playerController.IsDigging())
		{
            playerController.Move(cachedMoveX);
		}
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        // Move 액션이 Vector2라고 가정하고 x축을 사용
        Vector2 move = ctx.ReadValue<Vector2>();
        float newMoveX = move.x;
        
        // 이동 값 업데이트
        cachedMoveX = newMoveX;
        
        // 이동 입력을 서버로 전송
        bool isMovingLeft = newMoveX < -0.1f;
        bool isMovingRight = newMoveX > 0.1f;
        playerController.SendInputToServer(isMovingLeft, isMovingRight, false, false, false, false);
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        // 이동 중지
        cachedMoveX = 0f;
        playerController.SendInputToServer(false, false, false, false, false, false);
    }

    /// <summary> UI 버튼: 왼쪽 이동 누름 </summary>
    public void OnMoveLeftDown()
    {
        cachedMoveX = -1f;
        playerController.SendInputToServer(true, false, false, false, false, false);
    }

    /// <summary> UI 버튼: 왼쪽 이동 뗌 </summary>
    public void OnMoveLeftUp()
    {
        if (Mathf.Approximately(cachedMoveX, -1f)) cachedMoveX = 0f;
        playerController.SendInputToServer(false, false, false, false, false, false);
    }

    /// <summary> UI 버튼: 오른쪽 이동 누름 </summary>
    public void OnMoveRightDown()
    {
        cachedMoveX = 1f;
        playerController.SendInputToServer(false, true, false, false, false, false);
    }

    /// <summary> UI 버튼: 오른쪽 이동 뗌 </summary>
    public void OnMoveRightUp()
    {
        if (Mathf.Approximately(cachedMoveX, 1f)) cachedMoveX = 0f;
        playerController.SendInputToServer(false, false, false, false, false, false);
    }

    /// <summary> UI 버튼: Jump 액션과 동일 (점프) </summary>
    public void OnJumpButton()
    {
        playerController.Jump();
        bool isMovingLeft = cachedMoveX < -0.1f;
        bool isMovingRight = cachedMoveX > 0.1f;
        playerController.SendInputToServer(isMovingLeft, isMovingRight, true, false, false, false);
    }

    /// <summary> UI 버튼: Action1 액션과 동일 (리시브/디그, 이동 없음) </summary>
    public void OnDigButton()
    {
        playerController.Receive(cachedMoveX);
    }

    /// <summary> UI 버튼: Action2 액션과 동일 (토스/서브) </summary>
    public void OnTossButton()
    {
        playerController.Toss(cachedMoveX);
    }

    /// <summary> UI 버튼: Attack 액션과 동일 (스파이크) </summary>
    public void OnSpikeButton()
    {
        playerController.Spike(cachedMoveX);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        playerController.Jump();
        bool isMovingLeft = cachedMoveX < -0.1f;
        bool isMovingRight = cachedMoveX > 0.1f;
        playerController.SendInputToServer(isMovingLeft, isMovingRight, true, false, false, false);
    }

    private void OnHandPerformed(InputAction.CallbackContext ctx)
    {
        playerController.Receive(cachedMoveX); 
    }

    private void OnHandPerformed2(InputAction.CallbackContext ctx)
    {
        playerController.Toss(cachedMoveX);
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        playerController.Spike(cachedMoveX);
    }
}

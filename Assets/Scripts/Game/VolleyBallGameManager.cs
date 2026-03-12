using UnityEngine;
using UnityEngine.Events;

public class VolleyBallGameManager : MonoBehaviour
{
    public static VolleyBallGameManager instance;

    public UnityAction OnBallInited;

    [SerializeField] private Transform net;
    [SerializeField] private Transform ball;

    private int blueTeamScore;
    private int redTeamScore;

    private int touchCount;

    private bool isBallInBlueSide;

    private string lastTouchPlayerId;

    private bool isGameEnd;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        OnBallInited += ClearTouchCount;
    }

    void Update()
    {
        // Debug.Log("isBallInBlueSide: " + isBallInBlueSide + ", ballTouchCount: " + touchCount);

        CheckSide();
    }

    public bool IsGameEnd()
    {
        return isGameEnd;
    }

    public void EndGame()
    {
        isGameEnd = true;
    }

    public void RaiseScore(bool isBlueTeam)
    {
        if (isBlueTeam)
            blueTeamScore++;
        else
            redTeamScore++;

        // CheckScoreToGameEnd();
    }

    public Transform GetVolleyBall()
    {
        return ball;
    }

    public int GetBlueTeamScore()
    {
        return blueTeamScore;
    }

    public int GetRedTeamScore()
    {
        return redTeamScore;
    }

    public void CountTouch()
    {
        touchCount++;
    }

    public void ClearTouchCount()
    {
        touchCount = 0;
    }

    public int GetTouchCount()
    {
        return touchCount;
    }

    public void SetLastTouchPlayerId(string id)
    {
        lastTouchPlayerId = id;
    }

    public string GetLastTouchPlayerId()
    {
        return lastTouchPlayerId;
    }

    public void InitBall()
    {
        // 여기서 볼 초기화 로직 실행해야함
        // 지금은 VolleyBall에서 OnBallInited에 등록하여 사용중인 상황
        OnBallInited?.Invoke();
        SetLastTouchPlayerId(string.Empty);
    }

    // 게임 시작 시 볼을 활성화할 때 사용
    public void ActivateBall()
    {
        if (ball != null)
        {
            ball.gameObject.SetActive(true);
        }
    }

    public bool IsBallInBlueSide()
    {
        return isBallInBlueSide;
    }

    private void CheckSide()
    {
        float netXpos = net.position.x;
        if (ball.transform.position.x - netXpos < 0f)
            isBallInBlueSide = true;
        else
            isBallInBlueSide = false;
    }

    private void CheckScoreToGameEnd()
    {
        if (blueTeamScore == 7)
        {
            Debug.Log("블루팀 승리");
            VolleyBallSceneUIController.instance.ShowResultPanel(isBlueTeamWin: true);
            isGameEnd = true;
        }
        else if (redTeamScore == 7)
        {
            Debug.Log("레드팀 승리");
            VolleyBallSceneUIController.instance.ShowResultPanel(isBlueTeamWin: false);
            isGameEnd = true;
        }
    }
}

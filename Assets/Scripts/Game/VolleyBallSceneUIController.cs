using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VolleyBallSceneUIController : MonoBehaviour
{
    [Header("ScorePanel")]
    [SerializeField] private TMP_Text blueTeamScoreText;
    [SerializeField] private TMP_Text redTeamScoreText;

    [Header("ResultPanel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject blueTeamWinText;
    [SerializeField] private GameObject redTeamWinText;
    [SerializeField] private Button toLobbyButton;

    [Header("Timer")]
    [SerializeField] private TMP_Text timerText;

    [Header("Judge")]
    [SerializeField] private TMP_Text judgeText;
    private Coroutine judgeHideCoroutine;

    [Header("PlayerInputPanel")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button digButton;
    [SerializeField] private Button tossButton;
    [SerializeField] private Button spikeButton;
    [Tooltip("LeftButton, RightButton → Move 액션 (UI는 PlayerInputHandler 메서드로 처리). 스폰 시 InitializePlayerInputPanel로 할당됨.")]
    private PlayerInputHandler playerInputHandler;

    public static VolleyBallSceneUIController instance;

    /// <summary> 스폰 시 VolleyBallPlayerManager에서 호출. inputHandler를 할당하고 버튼 바인딩을 초기화합니다. </summary>
    public void InitializePlayerInputPanel(PlayerInputHandler handler)
    {
        playerInputHandler = handler;
        ClearPlayerInputPanelBindings();
        SetupPlayerInputPanel();
    }

    private void ClearPlayerInputPanelBindings()
    {
        if (leftButton != null)
        {
            var t = leftButton.gameObject.GetComponent<EventTrigger>();
            if (t != null && t.triggers != null) t.triggers.Clear();
        }
        if (rightButton != null)
        {
            var t = rightButton.gameObject.GetComponent<EventTrigger>();
            if (t != null && t.triggers != null) t.triggers.Clear();
        }
        if (jumpButton != null) jumpButton.onClick.RemoveAllListeners();
        if (digButton != null) digButton.onClick.RemoveAllListeners();
        if (tossButton != null) tossButton.onClick.RemoveAllListeners();
        if (spikeButton != null) spikeButton.onClick.RemoveAllListeners();
    }

    public void ShowResultPanel(bool isBlueTeamWin)
    {
        if (isBlueTeamWin)
            blueTeamWinText.SetActive(true);
        else
            redTeamWinText.SetActive(true);

        resultPanel.SetActive(true);
    }

    public void ShowResultPanel(int winTeam)
    {
        // 0 = 왼쪽(블루) 팀 승리, 1 = 오른쪽(레드) 팀 승리
        ShowResultPanel(winTeam == 0);
    }

    /// <summary>
    /// Judge 메시지 수신 시 호출. 0=왼쪽 팀 득점, 1=오른쪽 팀 득점. 2초 후 자동 숨김.
    /// </summary>
    public void ShowJudgeMessage(int teamValue)
    {
        if (judgeText == null) return;
        if (judgeHideCoroutine != null) StopCoroutine(judgeHideCoroutine);
        judgeText.text = teamValue == 1 ? "RIGHT TEAM SCORE!" : "LEFT TEAM SCORE!";
        judgeText.gameObject.SetActive(true);
        judgeHideCoroutine = StartCoroutine(HideJudgeAfterSeconds(2f));
    }

    private IEnumerator HideJudgeAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        judgeHideCoroutine = null;
        if (judgeText != null) judgeText.gameObject.SetActive(false);
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        toLobbyButton.onClick.AddListener(() =>
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            OnToLobbyButtonClicked();
        });
        SetupPlayerInputPanel();
    }

    private void SetupPlayerInputPanel()
    {
        if (playerInputHandler != null && leftButton != null)
            AddMoveButtonEvents(leftButton, true);
        if (playerInputHandler != null && rightButton != null)
            AddMoveButtonEvents(rightButton, false);

        if (jumpButton != null && playerInputHandler != null)
            jumpButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySfx("Btn_Click");
                playerInputHandler.OnJumpButton();
            });
        if (digButton != null && playerInputHandler != null)
            digButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySfx("Btn_Click");
                playerInputHandler.OnDigButton();
            });
        if (tossButton != null && playerInputHandler != null)
            tossButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySfx("Btn_Click");
                playerInputHandler.OnTossButton();
            });
        if (spikeButton != null && playerInputHandler != null)
            spikeButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySfx("Btn_Click");
                playerInputHandler.OnSpikeButton();
            });
    }

    private void AddMoveButtonEvents(Button button, bool isLeft)
    {
        var trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener(_ =>
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx("Btn_Click");
            if (isLeft) playerInputHandler.OnMoveLeftDown(); else playerInputHandler.OnMoveRightDown();
        });
        trigger.triggers.Add(entryDown);

        var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener(_ => { if (isLeft) playerInputHandler.OnMoveLeftUp(); else playerInputHandler.OnMoveRightUp(); });
        trigger.triggers.Add(entryUp);

        var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener(_ => { if (isLeft) playerInputHandler.OnMoveLeftUp(); else playerInputHandler.OnMoveRightUp(); });
        trigger.triggers.Add(entryExit);
    }

    void Update()
    {
        // 점수는 서버의 GameRoomState에서 직접 가져온다.
        if (blueTeamScoreText != null)
            blueTeamScoreText.text = GameServer.LeftTeamScore.ToString();
        if (redTeamScoreText != null)
            redTeamScoreText.text = GameServer.RightTeamScore.ToString();

        if (timerText != null)
        {
            SetTimerText();
        }
    }

    private void SetTimerText()
    {
        double startMs = GameServer.GameStartTimeUtcMs;
        if (startMs > 0)
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double elapsedMs = nowMs - startMs;
            if (elapsedMs < 0) elapsedMs = 0;
            int totalSeconds = (int)(elapsedMs / 1000);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = $"{minutes:D2}:{seconds:D2}";
        }
        else
        {
            timerText.text = "00:00";
        }
    }

    private void OnToLobbyButtonClicked()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}

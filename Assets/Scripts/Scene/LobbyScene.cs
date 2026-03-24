using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyScene : MonoBehaviour
{
    public static LobbyScene instance;

    [SerializeField] private GameObject matchingEndUI;

    private float matchingTimer = 0f;

    private bool isMatching = false;

    private void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isMatching) return;

        // 서버에서 타이머 값 받아올 예정
        // 종료 타이밍 또한 서버에서 받을 예정
        matchingTimer += Time.deltaTime;
        
        if (LobbyUIController.instance != null)
        {
            LobbyUIController.instance.UpdateTimer(matchingTimer);
        }

        if (matchingTimer <= 0)
        {
            isMatching = false;
            StartCoroutine(EndMatching());
        }
    }

    public void StartMatching()
    {
        isMatching = true;
    }

    private IEnumerator EndMatching()
    {
        // 서버에서 매칭 종료 신호 받으면 실행할 로직
        // 매칭 종료 ui 보여주기
        matchingEndUI.SetActive(true);

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("VolleyBallScene");
    }
}

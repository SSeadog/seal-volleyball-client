using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBase : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("(인트로 노출) 1초 후 HomeScene으로 이동");

        StartCoroutine(MoveToHomeScene());

        // 씬 전환 시 닉네임 초기화
        PlayerNickname.ClearNickname();
    }

    IEnumerator MoveToHomeScene()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("HomeScene");
    }
}

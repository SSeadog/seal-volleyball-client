using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeScene : MonoBehaviour
{
    public static HomeScene instance;

    private void Awake()
    {
        instance = this;

        SoundManager.Instance.PlayBgm("basic");
    }

    public void OnClickBtnGoToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}

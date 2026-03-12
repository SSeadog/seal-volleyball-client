using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerCardController : MonoBehaviour
{
    [SerializeField] private TMP_Text nameTxt;
    [SerializeField] private GameObject playerImage;
    [SerializeField] private Image background;

    public void SetName(string name)
    {
        if (nameTxt != null)
        {
            nameTxt.text = name;
        }
    }

    public void SetActive(bool isActive)
    {
        if (playerImage != null)
        {
            playerImage.SetActive(isActive);
        }

        if (background != null)
            background.color = isActive ? Color.white : Color.grey;
    }

    void Start()
    {
        SetActive(false);
    }
}

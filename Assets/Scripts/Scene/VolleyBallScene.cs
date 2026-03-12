using UnityEngine;
using UnityEngine.Serialization;

public class VolleyBallScene : MonoBehaviour
{
    public static VolleyBallScene Instance { get { return instance; } }
    private static VolleyBallScene instance;
    [SerializeField] private VolleyBallController volleyBallController;
    public VolleyBallController VolleyBallController { get { return volleyBallController; } }
    [Header("Effects")]
    [FormerlySerializedAs("difHandEffectPrefab")]
    [SerializeField] private GameObject digHandEffectPrefab;
    [SerializeField] private GameObject tossHandEffectPrefab;
    [SerializeField] private GameObject spikeHandEffectPrefab;
    // 자기는 맵, 플레이어 등 스폰 다 끝나면 서버로 준비 신호 알리기
    // 서버가 게임 시작 신호 보내줄 떄까지 대기
    
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SpawnReceiveHandEffect(Vector3 worldPosition)
    {
        if (digHandEffectPrefab == null) return;
        Instantiate(digHandEffectPrefab, worldPosition, Quaternion.identity);
    }

    public void SpawnTossHandEffect(Vector3 worldPosition)
    {
        if (tossHandEffectPrefab == null) return;
        Instantiate(tossHandEffectPrefab, worldPosition, Quaternion.identity);
    }

    public void SpawnSpikeHandEffect(Vector3 worldPosition)
    {
        if (spikeHandEffectPrefab == null) return;
        Instantiate(spikeHandEffectPrefab, worldPosition, Quaternion.identity);
    }
}

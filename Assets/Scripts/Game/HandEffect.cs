using UnityEngine;

public class HandEffect : MonoBehaviour
{
    [SerializeField] private float livingTime = 1f;
    [SerializeField] private float scaleAmplitude = 0.3f;
    [SerializeField] private float rotationAmplitude = 30f;

    private float elapsed;
    private Vector3 baseScale;
    private float baseRotationZ;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotationZ = transform.localEulerAngles.z;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (livingTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01(elapsed / livingTime);

        // 0 → 1 → 0 곡선으로 스케일/회전 변화 (sin 파형)
        float wave = Mathf.Sin(t * Mathf.PI);

        // Scale: 기본 스케일에서 amplitude만큼 커졌다가 다시 줄어듦
        float scaleFactor = 1f + scaleAmplitude * wave;
        transform.localScale = baseScale * scaleFactor;

        // RotationZ: 기준 각도에서 양수 → 0 → 음수로 흔들리는 느낌
        float offsetZ = rotationAmplitude * wave;
        Vector3 euler = transform.localEulerAngles;
        euler.z = baseRotationZ + offsetZ;
        transform.localEulerAngles = euler;

        if (elapsed >= livingTime)
        {
            Destroy(gameObject);
        }
    }
}



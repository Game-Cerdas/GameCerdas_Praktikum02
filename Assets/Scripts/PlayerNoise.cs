using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField]
    private float noiseRadius = 6f;

    [SerializeField]
    private float noiseDuration = 0.5f;

    private float noiseTimer = 0f;

    public bool IsMakingNoise =>
        noiseTimer > 0f;

    public float NoiseRadius =>
        noiseRadius;

    private void Update()
    {
        // Tekan SPACE untuk menghasilkan suara
        if (Input.GetKeyDown(KeyCode.Space))
        {
            noiseTimer = noiseDuration;
        }

        if (noiseTimer > 0f)
        {
            noiseTimer -= Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            noiseRadius
        );
    }
}
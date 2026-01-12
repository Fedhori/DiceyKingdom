using UnityEngine;

public sealed class ItemOrbitController : MonoBehaviour
{
    [SerializeField] private float radius = 64f;
    [SerializeField] private float periodSeconds = 4f;

    Transform center;
    float angleDegrees;
    float localZ;

    public void Initialize(Transform center, int index, int total, float radius, float periodSeconds)
    {
        this.center = center;
        this.radius = radius;
        this.periodSeconds = periodSeconds;
        localZ = transform.localPosition.z;

        int count = Mathf.Max(1, total);
        float step = 360f / count;
        angleDegrees = step * Mathf.Max(0, index);
        ApplyPosition();
    }

    void Update()
    {
        if (center == null)
            return;

        if (periodSeconds > 0f)
        {
            float delta = (360f / periodSeconds) * Time.deltaTime;
            angleDegrees = Mathf.Repeat(angleDegrees - delta, 360f);
        }

        ApplyPosition();
    }

    void ApplyPosition()
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float x = Mathf.Cos(rad) * radius;
        float y = Mathf.Sin(rad) * radius;

        if (transform.parent == center)
            transform.localPosition = new Vector3(x, y, localZ);
        else
            transform.position = new Vector3(center.position.x + x, center.position.y + y, transform.position.z);
    }
}

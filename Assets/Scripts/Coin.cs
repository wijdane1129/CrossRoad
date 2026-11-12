using UnityEngine;

// Simple spinning/bobbing coin. Collection is handled by GameManager
// (grid based), so this only does the visuals.
public class Coin : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 180f;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 3f;

    private float baseY;

    private void Start()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = p;
    }
}
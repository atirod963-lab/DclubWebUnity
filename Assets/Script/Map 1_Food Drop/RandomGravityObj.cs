using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RandomGravityObj : MonoBehaviour
{
    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = Random.Range(20f, 50f);

        Destroy(gameObject, 5f);
    }
}
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ConnectionPoint : MonoBehaviour
{
    public enum Direction { Up, Down }
    public Direction pointDirection;

    [HideInInspector]
    public Collider2D parentCollider;

    void Awake()
    {
        if(transform.parent.GetComponent<Collider2D>() != null)
        {
            parentCollider = transform.parent.GetComponent<Collider2D>();
        }
    }
}
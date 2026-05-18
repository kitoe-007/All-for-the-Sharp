using UnityEngine;

public class Taskmove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool ismove = false;
    public Transform targetPoint;      // Целевая точка для перемещения
    private Vector2 BasePoint;
    public float smoothTime = 0.3f; 
    // Время достижения цели (чем больше, тем дольше)
    private Vector2 velocity = Vector2.zero; // Это ref-переменная, её использует SmoothDamp для своей р
    void Start()
    {
        BasePoint = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (ismove)
        {
            transform.position = Vector2.SmoothDamp(transform.position, targetPoint.position, ref velocity, smoothTime);
        }
        else
        {
            transform.position = Vector2.SmoothDamp(transform.position, BasePoint, ref velocity, smoothTime);
        }
    }

    public void Move()
    {
        ismove = !ismove;
    }

    
}

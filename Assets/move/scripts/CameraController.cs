using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    private Vector3 pos;

    private void Awake()
    {
        if (!player)
            player = FindAnyObjectByType<hero>().transform;
    }
    private void Update()
    {
        pos = player.position;
        pos.z = -10f;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime); //плавное движение
    }



    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}
}

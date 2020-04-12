using System;
using UnityEngine;

public class TargetMovementScript : MonoBehaviour
{
    [SerializeField] float targetMoveSpeed = 4f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.localPosition.x < 12)
        {
            transform.localPosition = new Vector3(transform.localPosition.x + targetMoveSpeed * 0.01f, 1.5f, 0);
        }
        else if (transform.localPosition.x < 21)
        {
            transform.localPosition = new Vector3(transform.localPosition.x + targetMoveSpeed * 0.01f,
                1.5f + (0.31f * (transform.localPosition.x - 12)), 0);
        }
        else if (transform.localPosition.x < 80)
        {
            transform.localPosition = new Vector3(transform.localPosition.x + targetMoveSpeed * 0.01f,
                transform.localPosition.y,
                (float) Math.Sin(transform.localPosition.x - 21) * 2);
        }
    }
}
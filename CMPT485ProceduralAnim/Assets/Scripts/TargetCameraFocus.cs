using UnityEngine;

public class TargetCameraFocus : MonoBehaviour
{
    [SerializeField] GameObject secondaryTarget;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation =
            Quaternion.LookRotation( secondaryTarget.transform.position - transform.position, Vector3.up);
    }
}

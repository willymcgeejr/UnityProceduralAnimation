using System.Collections;
using UnityEngine;

public class LegStepper : MonoBehaviour
{

    // The position and rotation we want to stay in range of
    public Transform homeTransform;
    // Stay within this distance of home
    [SerializeField] float wantStepAtDistance = 2.5F;
    // How long a step takes to complete
    [SerializeField] float moveDuration = 0.5F;
    // Fraction of the max distance from home we want to overshoot by
    [SerializeField] float stepOvershootFraction = 0.5F;


    // Is the leg moving?
    public bool Moving;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    public void TryMove()
    {
        // If we are already moving, don't start another move
        if (Moving) return;

        float distFromHome = Vector3.Distance(transform.position, homeTransform.position);

        // If we are too far off in position or rotation
        if (distFromHome > wantStepAtDistance)
        {
            // Start the step coroutine
            StartCoroutine(Move());
        }
    }

    IEnumerator Move()
    {
        Moving = true;

        Vector3 startPoint = transform.position;
        Quaternion startRot = transform.rotation;

        Quaternion endRot = homeTransform.rotation;

        // Directional vector from the foot to the home position
        Vector3 towardHome = (homeTransform.position - transform.position);
        // Total distnace to overshoot by   
        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = towardHome * overshootDistance;
        // Since we don't ground the point in this simplified implementation,
        // we restrict the overshoot vector to be level with the ground
        // by projecting it on the world XZ plane.
        overshootVector = Vector3.ProjectOnPlane(overshootVector, Vector3.up);

        // Apply the overshoot
        Vector3 endPoint = homeTransform.position + overshootVector;

        // We want to pass through the center point
        Vector3 centerPoint = (startPoint + endPoint) / 2;
        // But also lift off, so we move it up by half the step distance (arbitrarily)
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

        float timeElapsed = 0;
        do
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveDuration;
            normalizedTime = Easing.Cubic.InOut(normalizedTime);

            // Quadratic bezier curve
            transform.position =
              Vector3.Lerp(
                Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                normalizedTime
              );

            transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            yield return null;
        }
        while (timeElapsed < moveDuration);

        Moving = false;
    }


}

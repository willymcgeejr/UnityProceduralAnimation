using System;
using System.Collections;
using UnityEngine;

public class GeckoController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform headBone;
    [SerializeField] Transform tail;
    [SerializeField] Transform yRotationTransform;
    [SerializeField] float headMaxTurnAngle = 65.0f;
    [SerializeField] float headTrackingSpeed = 1.0f;
    [SerializeField] Transform leftEyeBone;
    [SerializeField] Transform rightEyeBone;
    [SerializeField] float eyeTrackingSpeed = 10.0f;
    [SerializeField] float leftEyeMaxYRotation = 30.0f;
    [SerializeField] float leftEyeMinYRotation = 0;
    [SerializeField] float rightEyeMaxYRotation = 30.0f;
    [SerializeField] float rightEyeMinYRotation = 0;
    [SerializeField] LegStepper frontLeftLegStepper;
    [SerializeField] LegStepper frontRightLegStepper;
    [SerializeField] LegStepper backLeftLegStepper;

    [SerializeField] LegStepper backRightLegStepper;

    // How fast we can turn and move full throttle
    [SerializeField] float turnSpeed = 100.0f;
    [SerializeField] float moveSpeed = 3.5f;

    // How fast we will reach the above speeds
    [SerializeField] float turnAcceleration = 3.0f;
    [SerializeField] float moveAcceleration = 0.5f;

    // Try to stay in this range from the target
    [SerializeField] float minDistToTarget = 1.0f;
    [SerializeField] float maxDistToTarget = 4.0f;

    // If we are above this angle from the target, start turning
    [SerializeField] float maxAngToTarget = 5.0f;
    [SerializeField] bool checkFloor;
    [SerializeField] float smoothingSpeed = 20.0f;

    // World space velocity
    Vector3 currentVelocity;

    // We are only doing a rotation around the up axis, so we only use a float here
    float currentAngularVelocity;
    LegStepper[] feet;
    Vector3 defaultFL = new Vector3(-0.5f, -0.3f, 0);
    Vector3 defaultFR = new Vector3(0.5f, -0.3f, 0);
    Vector3 defaultBL = new Vector3(-0.5f, -0.3f, -1.5f);
    Vector3 defaultBR = new Vector3(0.5f, -0.3f, -1.5f);


    void Awake()
    {
        StartCoroutine(LegUpdateCoroutine());
        feet = new LegStepper[] {backLeftLegStepper, backRightLegStepper, frontRightLegStepper, frontLeftLegStepper};
    }

    // Update is called once per frame
    void LateUpdate()
    {
        RootMotionUpdate();
        HeadTrackingUpdate();
        EyeTrackingUpdate();
        if (checkFloor)
        {
            FootHeightUpdate();
        }
    }

    void HeadTrackingUpdate()
    {
        // Store the current head rotation since we will be resetting it
        Quaternion currentLocalRotation = headBone.localRotation;
        // Reset the head rotation so our world to local space transformation will use the head's zero rotation. 
        // Note: Quaternion.Identity is the quaternion equivalent of "zero"
        headBone.localRotation = Quaternion.identity;

        Vector3 targetWorldLookDir = target.position - headBone.position;
        Vector3 targetLocalLookDir = headBone.InverseTransformDirection(targetWorldLookDir);

        // Apply angle limit
        targetLocalLookDir = Vector3.RotateTowards(
            Vector3.forward,
            targetLocalLookDir,
            Mathf.Deg2Rad * headMaxTurnAngle, // Note we multiply by Mathf.Deg2Rad here to convert degrees to radians
            0 // We don't care about the length here, so we leave it at zero
        );

        // Get the local rotation by using LookRotation on a local directional vector
        Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

        // Apply smoothing
        headBone.localRotation = Quaternion.Slerp(
            currentLocalRotation,
            targetLocalRotation,
            1 - Mathf.Exp(-headTrackingSpeed * Time.deltaTime));
    }

    void EyeTrackingUpdate()
    {
        Quaternion targetEyeRotation = Quaternion.LookRotation(
            target.position - headBone.position, // toward target
            transform.up
        );

        leftEyeBone.rotation = Quaternion.Slerp(
            leftEyeBone.rotation,
            targetEyeRotation,
            1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        rightEyeBone.rotation = Quaternion.Slerp(
            rightEyeBone.rotation,
            targetEyeRotation,
            1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime)
        );

        float leftEyeCurrentYRotation = leftEyeBone.localEulerAngles.y;
        float rightEyeCurrentYRotation = rightEyeBone.localEulerAngles.y;

        // Move the rotation to a -180 ~ 180 range
        if (leftEyeCurrentYRotation > 180)
        {
            leftEyeCurrentYRotation -= 360;
        }

        if (rightEyeCurrentYRotation > 180)
        {
            rightEyeCurrentYRotation -= 360;
        }

        // Clamp the Y axis rotation
        float leftEyeClampedYRotation = Mathf.Clamp(
            leftEyeCurrentYRotation,
            leftEyeMinYRotation,
            leftEyeMaxYRotation
        );
        float rightEyeClampedYRotation = Mathf.Clamp(
            rightEyeCurrentYRotation,
            rightEyeMinYRotation,
            rightEyeMaxYRotation
        );

        // Apply the clamped Y rotation without changing the X and Z rotations
        leftEyeBone.localEulerAngles = new Vector3(
            leftEyeBone.localEulerAngles.x,
            leftEyeClampedYRotation,
            leftEyeBone.localEulerAngles.z
        );
        rightEyeBone.localEulerAngles = new Vector3(
            rightEyeBone.localEulerAngles.x,
            rightEyeClampedYRotation,
            rightEyeBone.localEulerAngles.z
        );
    }

    void FootHeightUpdate()
    {
        float avgHeight = 0;
        for (int i = 0; i < feet.Length; i++)
        {
            avgHeight += feet[i].homeTransform.transform.position.y;
        }

        avgHeight = avgHeight / 4;
        if (Math.Abs(transform.position.y - avgHeight - 0.5) > 0.01)
        {
            transform.position = new Vector3(transform.position.x, avgHeight - 0.5f,
                transform.position.z);
        }

        Vector3[] normals = new Vector3[4];
        for (int i = 0; i < feet.Length; i++)
        {
            var footHome = feet[i].homeTransform;
            var hitInfo1 = new RaycastHit();
            if (Physics.Raycast(footHome.transform.position + footHome.up * 1f, -footHome.up, out hitInfo1))
            {
                footHome.position = hitInfo1.point + footHome.up * 0.15f;
                if (Math.Abs(hitInfo1.normal.normalized.x - footHome.up.normalized.x) > 0.2 ||
                    Math.Abs(hitInfo1.normal.normalized.z - footHome.up.normalized.z) > 0.2)
                {
                    footHome.rotation = Quaternion.LookRotation(footHome.forward, hitInfo1.normal);
                    footHome.rotation = Quaternion.LookRotation(footHome.right, hitInfo1.normal);
                    footHome.transform.Rotate(0, -footHome.localRotation.eulerAngles.y, 0);
                }

                normals[i] = (hitInfo1.normal);
                Debug.DrawRay(footHome.position, footHome.up * 100, Color.blue);
                Debug.DrawRay(hitInfo1.point, hitInfo1.normal * 100, Color.red);
            }
        }

        Vector3 bodyNormal = Vector3.zero;
        for (int i = 0; i < normals.Length; i++)
        {
            bodyNormal += normals[i];
        }

        float yRot = transform.eulerAngles.y;
        Quaternion tempRotation = transform.rotation;
        transform.rotation = Quaternion.LookRotation(transform.forward, bodyNormal);
        transform.rotation = Quaternion.LookRotation(transform.right, bodyNormal);
        transform.Rotate(0, yRot - transform.localRotation.eulerAngles.y, 0);
        transform.rotation =
            Quaternion.RotateTowards(tempRotation, transform.rotation, smoothingSpeed * Time.deltaTime);
    }

    void RootMotionUpdate()
    {
        // Get the direction toward our target
        Vector3 towardTarget = target.position - transform.position;
        // Vector toward target on the local XZ plane
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, Vector3.up);
        // Get the angle from the gecko's forward direction to the direction toward toward our target
        // Here we get the signed angle around the up vector so we know which direction to turn in
        float angToTarget =
            Vector3.SignedAngle(yRotationTransform.transform.forward, towardTargetProjected, Vector3.up);
        float targetAngularVelocity = 0;

        // If we are within the max angle (i.e. approximately facing the target)
        // leave the target angular velocity at zero
        if (Mathf.Abs(angToTarget) > maxAngToTarget)
        {
            // Angles in Unity are clockwise, so a positive angle here means to our right
            if (angToTarget > 0)
            {
                targetAngularVelocity = turnSpeed;
            }
            // Invert angular speed if target is to our left
            else
            {
                targetAngularVelocity = -turnSpeed;
            }
        }

        // Use our smoothing function to gradually change the velocity
        currentAngularVelocity = Mathf.Lerp(
            currentAngularVelocity,
            targetAngularVelocity,
            1 - Mathf.Exp(-turnAcceleration * Time.deltaTime)
        );
        Debug.Log(currentAngularVelocity);
        // Rotate the transform around the Y axis in world space, 
        // making sure to multiply by delta time to get a consistent angular velocity

        yRotationTransform.transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0);
        Vector3 targetVelocity = Vector3.zero;

        // Don't move if we're facing away from the target, just rotate in place
        if (Mathf.Abs(angToTarget) < 75)
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);

            // If we're too far away, approach the target
            if (distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            // If we're too close, reverse the direction and move away
            else if (distToTarget < minDistToTarget)
            {
                targetVelocity = moveSpeed * -towardTargetProjected.normalized;
            }
        }

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            1 - Mathf.Exp(-moveAcceleration * Time.deltaTime)
        );

        // Apply the velocity
        transform.position += currentVelocity * Time.deltaTime;
    }


    // Only allow diagonal leg pairs to step together
    IEnumerator LegUpdateCoroutine()
    {
        // Run continuously
        while (true)
        {
            // Try moving one diagonal pair of legs
            do
            {
                frontLeftLegStepper.TryMove();
                backRightLegStepper.TryMove();
                // Wait a frame
                yield return null;

                // Stay in this loop while either leg is moving.
                // If only one leg in the pair is moving, the calls to TryMove() will let
                // the other leg move if it wants to.
            } while (backRightLegStepper.Moving || frontLeftLegStepper.Moving);

            // Do the same thing for the other diagonal pair
            do
            {
                frontRightLegStepper.TryMove();
                backLeftLegStepper.TryMove();
                yield return null;
            } while (backLeftLegStepper.Moving || frontRightLegStepper.Moving);
        }
    }
}
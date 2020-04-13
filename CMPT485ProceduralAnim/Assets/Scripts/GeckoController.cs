using System;
using System.Collections;
using UnityEngine;

public class GeckoController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform headBone;
    [SerializeField] float headMaxTurnAngle = 65.0f;
    [SerializeField] float headTrackingSpeed = 1.0f;

    // Parameters for eye rotation
    [SerializeField] Transform leftEyeBone;
    [SerializeField] Transform rightEyeBone;
    [SerializeField] float eyeTrackingSpeed = 10.0f;
    [SerializeField] float leftEyeMaxYRotation = 30.0f;
    [SerializeField] float leftEyeMinYRotation = 0;
    [SerializeField] float rightEyeMaxYRotation = 30.0f;
    [SerializeField] float rightEyeMinYRotation = 0;

    // The LegStepper objects for each foot
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
    [SerializeField] float minDistToTarget = 2.0f;
    [SerializeField] float maxDistToTarget = 4.5f;

    // If we are above this angle from the target, start turning
    [SerializeField] float maxAngToTarget = 5.0f;

    // Whether or not the gecko adjusts to the floor below it
    [SerializeField] bool checkFloor;

    // Speed at which the gecko body adjusts to meet the normal of the ground
    [SerializeField] float smoothingSpeed = 20.0f;

    // World space velocity
    Vector3 currentVelocity;

    // We are only doing a rotation around the up axis, so we only use a float here
    float currentAngularVelocity;

    // Storage for all of the LegSteppers that are used in height/rotation calculations
    LegStepper[] feet;

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
            HeightUpdate();
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
            0); // We don't care about the length here, so we leave it at zero

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

    void HeightUpdate()
    {
        // Store the average height of all four feet
        float avgHeight = 0;
        for (int i = 0; i < feet.Length; i++)
        {
            avgHeight += feet[i].homeTransform.transform.position.y;
        }

        avgHeight = avgHeight / feet.Length;

        // Update the height of the gecko according to the height of its feet
        transform.position = new Vector3(transform.position.x, avgHeight - 0.5f, transform.position.z);

        // Create storage for the normals of each of the gecko's feet
        Vector3[] normals = new Vector3[feet.Length];

        // Now that we've updated the model's height, we want to update the height and rotation of the feet
        for (int i = 0; i < feet.Length; i++)
        {
            // Here we want to update the home transforms of the feet, not the targets;
            // the resulting home transform will change the angle and height of the target on the next step
            Transform footHome = feet[i].homeTransform;
            RaycastHit hitInfo = new RaycastHit();

            // Generate a raycast from above the current home position and update the position on a hit
            if (Physics.Raycast(footHome.transform.position + footHome.up * 1f, -footHome.up, out hitInfo))
            {
                // Relocate the home position for the foot just above the hit point
                // (only so the hand mesh isn't partially sunken into the floor)
                footHome.position = hitInfo.point + footHome.up * 0.15f;

                // Adjust the rotation of the new position so the home's normal matches the hit's normal
                footHome.rotation = Quaternion.LookRotation(footHome.forward, hitInfo.normal);
                footHome.rotation = Quaternion.LookRotation(footHome.right, hitInfo.normal);

                // Once the x/z rotations are correct, cancel the resulting y rotation (otherwise feet will be twisted!)
                footHome.transform.Rotate(0, -footHome.localRotation.eulerAngles.y, 0);

                // Store the normal of the current foot
                normals[i] = (hitInfo.normal);

                //Draw a ray in the scene view to show the normal of the foot
                Debug.DrawRay(footHome.position, footHome.up * 100, Color.blue);
            }
        }

        // Record the average of all of the normals of the feet
        Vector3 bodyNormal = Vector3.zero;
        for (int i = 0; i < normals.Length; i++)
        {
            bodyNormal += normals[i];
        }

        // Figure out what rotations are needed to align the body with the combined feet normals,
        // then apply a smoothing function to prevent the body from 'snapping' into the new rotation
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
        float angToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, Vector3.up);
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

        // Rotate the transform around the extra Y transform
        // (if we attempt to rotate in world space, the model will get jammed on any non-flat surface), 
        // making sure to multiply by delta time to get a consistent angular velocity
        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0);
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
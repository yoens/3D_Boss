using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -3f);

    [Header("Look At")]
    [SerializeField] private float lookAtHeight = 0.5f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float wallPadding = 0.15f;

    [Header("Camera Distance")]
    [SerializeField] private float returnSpeed = 4f;

    private float currentDistance;
    private bool isInitialized;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookAtPosition =
            target.position + Vector3.up * lookAtHeight;

        Vector3 rotatedOffset = target.rotation * offset;

        Vector3 desiredPosition =
            target.position + rotatedOffset;

        Vector3 cameraVector =
            desiredPosition - lookAtPosition;

        float desiredDistance = cameraVector.magnitude;

        if (desiredDistance < 0.001f)
        {
            return;
        }

        Vector3 cameraDirection =
            cameraVector / desiredDistance;

        if (!isInitialized)
        {
            currentDistance = desiredDistance;
            isInitialized = true;
        }

        float allowedDistance = desiredDistance;

        if (Physics.SphereCast(
            lookAtPosition,
            collisionRadius,
            cameraDirection,
            out RaycastHit hit,
            desiredDistance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore))
        {
            allowedDistance = Mathf.Max(
                0f,
                hit.distance - wallPadding
            );
        }


        if (allowedDistance < currentDistance)
        {
            currentDistance = allowedDistance;
        }
        else
        {
            currentDistance = Mathf.MoveTowards(
                currentDistance,
                allowedDistance,
                returnSpeed * Time.deltaTime
            );
        }

        currentDistance = Mathf.Min(
            currentDistance,
            allowedDistance
        );

        transform.position =
            lookAtPosition + cameraDirection * currentDistance;

        Vector3 lookDirection =
            lookAtPosition - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(lookDirection);
        }
    }
}
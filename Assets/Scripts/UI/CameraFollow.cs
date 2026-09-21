using UnityEngine;


public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 rotatedOffset = target.rotation * offset;
        Vector3 targetPosition = target.position + rotatedOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        transform.rotation = Quaternion.LookRotation(target.position - transform.position);
    }
}
using UnityEngine;

public class Temporary_TPS_Camera : MonoBehaviour
{
    public string entityName;
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public float followSpeed = 10f;
    public float rotationSpeed = 10f;
    public bool lookAtTarget = true;
    public Vector3 lookAtPointOffset = new Vector3(0f, 1f, 0f); // 바라볼 지점 보정
    public Vector2 angleOffset; // x: pitch(상하), y: yaw(좌우) 추가 각도

    private Transform _targetTransform;

    private void Update()
    {
        if (!_targetTransform)
        {
            var obj = GameObject.Find($"{entityName}_Model");

            if (obj)
                _targetTransform = obj.transform;
        }
    }

    private void LateUpdate()
    {
        if (!_targetTransform)
            return;

        Vector3 desiredPosition = _targetTransform.position
                                  + _targetTransform.right * offset.x
                                  + _targetTransform.up * offset.y
                                  + _targetTransform.forward * offset.z;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        if (lookAtTarget)
        {
            Vector3 lookPoint = _targetTransform.position
                                + _targetTransform.right * lookAtPointOffset.x
                                + _targetTransform.up * lookAtPointOffset.y
                                + _targetTransform.forward * lookAtPointOffset.z;

            Quaternion baseRotation = Quaternion.LookRotation(lookPoint - transform.position);
            Quaternion angleAdjust = Quaternion.Euler(angleOffset.x, angleOffset.y, 0f);
            Quaternion desiredRotation = baseRotation * angleAdjust;

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }
    }
}

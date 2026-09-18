using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CenterOfMass : MonoBehaviour
{
    [SerializeField] private Transform centerOfMassTransform;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (centerOfMassTransform == null)
        {
            Debug.LogWarning($"[{name}] Center of mass transform is not assigned.");
            return;
        }

        rb.centerOfMass = centerOfMassTransform.localPosition;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (centerOfMassTransform == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(centerOfMassTransform.position, 0.05f);
    }
#endif
}
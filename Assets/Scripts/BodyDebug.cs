using UnityEngine;

public class BodyDebug : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Body hit by {other.name}");
    }
}
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
using Unity.Mathematics;
using UnityEngine;

public class LockRotation : MonoBehaviour
{
    

   

    void LateUpdate()
    {
        transform.rotation = quaternion.identity; // Reapply initial rotation
    }
}

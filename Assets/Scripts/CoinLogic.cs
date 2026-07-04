using UnityEngine;

public class CoinLogic : MonoBehaviour
{
    void Update()
    {
        // Xoay đồng xu liên tục
        transform.Rotate(Vector3.up * 180f * Time.deltaTime, Space.Self);
    }
}

using UnityEngine;
using Unity.Netcode;

public class NetworkManagerBootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}

using Unity.Netcode;
using UnityEngine;

public class NetworkBootTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("NetworkBootTest Start");
    }

    public void StartHost()
    {
        Debug.Log("StartHost called");
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        Debug.Log("StartClient called");
        NetworkManager.Singleton.StartClient();
    }
}

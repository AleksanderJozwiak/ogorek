using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            LobbyManager.Instance.CreateLobby("TestLobby");


        if (Input.GetKeyDown(KeyCode.F))
            LobbyManager.Instance.RequestLobbyList();

    }
}

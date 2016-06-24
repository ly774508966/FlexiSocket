// ************************
// Author: Sean Cheung
// Create: 2016/06/07/10:31
// Modified: 2016/06/08/15:02
// ************************

using System.Collections;
using System.Text;
using FlexiFramework.Networking;
using UnityEngine;

public class Server : MonoBehaviour
{
    private ISocketServer _server;

    // Use this for initialization
    private IEnumerator Start()
    {
        _server = FlexiSocket.Create(1366, Protocol.LengthPrefix);
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.ReceivedFromClient += OnReceivedFromClient;
        _server.SentToClient += OnSentToClient;
        _server.StartListen(10);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1, 5));
            foreach (var client in _server.Clients)
            {
                client.Send(Encoding.UTF8.GetBytes("Random message"));
            }
        }
    }

    private void OnSentToClient(bool success, ISocketClientToken client)
    {
        if (success)
            Debug.Log(string.Format("<color=red>Sent to client({0})</color>", client.ID));
    }

    private void OnReceivedFromClient(ISocketClientToken client, byte[] message)
    {
        Debug.Log(string.Format("<color=red>Received from client({0}): {1}</color>", client.ID,
            Encoding.UTF8.GetString(message)));
    }

    private void OnClientDisconnected(ISocketClientToken client)
    {
        Debug.Log(string.Format("<color=red>Client({0}) disconnected. Connected clients: {1}</color>", client.ID,
            _server.Clients.Count));
    }

    private void OnClientConnected(ISocketClientToken client)
    {
        Debug.Log(string.Format("<color=red>Client({0}) connected. Connected clients: {1}</color>", client.ID,
            _server.Clients.Count));
        client.Send(Encoding.UTF8.GetBytes("welcome"));
        /*try
        {
            GameObject.CreatePrimitive(PrimitiveType.Cube); //should be called from main thread
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }*/
    }

    private void OnDestroy()
    {
        _server.Close();
        _server = null;
    }

    private void Reset()
    {
        name = "Server";
    }
}
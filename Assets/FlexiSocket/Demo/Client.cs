// ************************
// Author: Sean Cheung
// Create: 2016/06/07/10:31
// Modified: 2016/06/08/14:46
// ************************

using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using FlexiFramework.Networking;
using UnityEngine;

public class Client : MonoBehaviour
{
    private ISocketClient _client;

    // Use this for initialization
    private IEnumerator Start()
    {
        _client = FlexiSocket.Create("localhost", 1366, Protocols.BodyLengthPrefix);
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
        _client.Received += OnReceived;
        _client.Sent += OnSent;
        yield return new WaitForSeconds(1);
        _client.Connect();
    }

    private void OnSent(bool success, Exception exception, SocketError error)
    {
        if (success)
            Debug.Log("Sent to server" , this);
    }

    private void OnReceived(bool success, Exception exception, SocketError error, byte[] message)
    {
        if (success)
            Debug.Log("Received from server: " + Encoding.UTF8.GetString(message), this);
    }

    private void OnDisconnected(bool success, Exception exception)
    {
        if (success)
            Debug.Log("Disconnected", this);
    }

    private void OnConnected(bool success, Exception exception)
    {
        Debug.Log("Connecting result: " + success, this);
        if (success)
            _client.Send(Encoding.UTF8.GetBytes("Let me join"));
    }

    private void OnDestroy()
    {
        _client.Close();
        _client = null;
    }

    private void Reset()
    {
        name = "Client";
    }
}
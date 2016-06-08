// ************************
// Author: Sean Cheung
// Create: 2016/06/08/11:21
// Modified: 2016/06/08/15:02
// ************************

using System.Collections;
using System.Net.Sockets;
using System.Text;
using FlexiFramework.Networking;
using UnityEngine;

public class AsyncClient : MonoBehaviour
{
    private ISocketClient _client;

    private IEnumerator Start()
    {
        _client = FlexiSocket.Create("127.0.0.1", 1366);
        yield return new WaitForSeconds(1); // wait for server to startup

        var connect = _client.ConnectAsync();
        yield return connect;
        if (!connect.IsSuccessful)
        {
            Debug.LogException(connect.Exception);
            yield break;
        }
        Debug.Log("Connected", this);
        while (true)
        {
            var receive = _client.ReceiveAsync();
            yield return receive;

            if (!receive.IsSuccessful)
            {
                if (receive.Exception != null)
                    Debug.LogException(receive.Exception);
                if (receive.Error != SocketError.Success)
                    Debug.LogError(receive.Error);
                _client.Close();
                yield break;
            }

            Debug.Log("Client received: " + Encoding.UTF8.GetString(receive.Data), this);

            var msg = Encoding.UTF8.GetBytes("Hey I've got your message");

            var send = _client.SendAsync(msg);
            yield return send;
            if (!send.IsSuccessful)
            {
                if (send.Exception != null)
                    Debug.LogException(send.Exception);
                if (send.Error != SocketError.Success)
                    Debug.LogError(send.Error);
                _client.Close();
                yield break;
            }
            Debug.Log("Message sent", this);
            GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        }
    }

    private void OnDestroy()
    {
        _client.Close();
        _client = null;
    }

    private void Reset()
    {
        name = "AsyncClient";
    }
}
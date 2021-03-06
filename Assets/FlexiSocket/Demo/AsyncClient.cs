﻿// ************************
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
        _client = FlexiSocket.Create("::1", 1366, Protocols.BodyLengthPrefix); //ipv6
        yield return new WaitForSeconds(1); // wait for server to startup since bot server and clients are in the same scene

        using (var connect = _client.ConnectAsync())
        {
            yield return connect;
            if (!connect.IsSuccessful)
            {
                Debug.LogException(connect.Exception);
                yield break;
            }
            Debug.Log("Connected", this);
        }
       
        while (_client.IsConnected)
        {
            using (var receive = _client.ReceiveAsync())
            {
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
            }

            var send = _client.SendAsync("Hey I've got your message");
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
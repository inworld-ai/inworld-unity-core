#if UNITY_WEBGL && !UNITY_EDITOR 
using System;

namespace Inworld
{
    public class WebSocket : IWebSocket
    {
        public string Address { get; private set; }
        public string[] SubProtocols { get; private set; }
        public WebSocketState ReadyState { get { return (WebSocketState)WebSocketManager.InworldWebSocketGetState(instanceId); } }
        public string BinaryType { get; set; } = "arraybuffer";

        public event EventHandler<OpenEventArgs> OnOpen;
        public event EventHandler<CloseEventArgs> OnClose;
        public event EventHandler<ErrorEventArgs> OnError;
        public event EventHandler<MessageEventArgs> OnMessage;

        internal int instanceId = 0;

        public WebSocket(string address)
        {
            this.Address = address;
            AllocateInstance();
        }

        public WebSocket(string address, string subProtocol)
        {
            this.Address = address;
            this.SubProtocols = new string[] { subProtocol };
            AllocateInstance();
        }

        public WebSocket(string address, string[] subProtocols)
        {
            this.Address = address;
            this.SubProtocols = subProtocols;
            AllocateInstance();
        }

        internal void AllocateInstance()
        {
            instanceId = WebSocketManager.AllocateInstance(this.Address, this.BinaryType);
            Log($"Allocate socket with instanceId: {instanceId}");
            if (this.SubProtocols == null) return;
            foreach (var protocol in this.SubProtocols)
            {
                if (string.IsNullOrEmpty(protocol)) continue;
                Log($"Add Sub Protocol {protocol}, with instanceId: {instanceId}");
                int code = WebSocketManager.InworldWebSocketAddSubProtocol(instanceId, protocol);
                if (code < 0)
                {
                    HandleOnError(GetErrorMessageFromCode(code));
                    break;
                }
            }
        }

        ~WebSocket()
        {
            Log($"Free socket with instanceId: {instanceId}");
            WebSocketManager.InworldWebSocketFree(instanceId);
        }

        public void ConnectAsync()
        {
            Log($"Connect with instanceId: {instanceId}");
            WebSocketManager.Add(this);
            int code = WebSocketManager.InworldWebSocketConnect(instanceId);
            if (code < 0) HandleOnError(GetErrorMessageFromCode(code));
        }

        public void CloseAsync()
        {
            Log($"Close with instanceId: {instanceId}");
            int code = WebSocketManager.InworldWebSocketClose(instanceId, (int)CloseStatusCode.Normal, "Normal Closure");
            if (code < 0) HandleOnError(GetErrorMessageFromCode(code));
        }

        public void SendAsync(string text)
        {
            Log($"Send, type: {Opcode.Text}, size: {text.Length}");
            int code = WebSocketManager.InworldWebSocketSendStr(instanceId, text);
            if (code < 0) HandleOnError(GetErrorMessageFromCode(code));
        }

        public void SendAsync(byte[] data)
        {
            Log($"Send, type: {Opcode.Binary}, size: {data.Length}");
            int code = WebSocketManager.InworldWebSocketSend(instanceId, data, data.Length);
            if (code < 0) HandleOnError(GetErrorMessageFromCode(code));
        }

        internal void HandleOnOpen()
        {
            Log("OnOpen");
            OnOpen?.Invoke(this, new OpenEventArgs());
        }

        internal void HandleOnMessage(byte[] rawData)
        {
            Log($"OnMessage, type: {Opcode.Binary}, size: {rawData.Length}");
            OnMessage?.Invoke(this, new MessageEventArgs(Opcode.Binary, rawData));
        }

        internal void HandleOnMessageStr(string data)
        {
            Log($"OnMessage, type: {Opcode.Text}, size: {data.Length}");
            OnMessage?.Invoke(this, new MessageEventArgs(Opcode.Text, data));
        }

        internal void HandleOnClose(ushort code, string reason)
        {
            Log($"OnClose, code: {code}, reason: {reason}");
            OnClose?.Invoke(this, new CloseEventArgs(code, reason));
            WebSocketManager.Remove(instanceId);
        }

        internal void HandleOnError(string msg)
        {
            Log("OnError, error: " + msg);
            OnError?.Invoke(this, new ErrorEventArgs(msg));
        }

        internal static string GetErrorMessageFromCode(int errorCode)
        {
            switch (errorCode)
            {
                case -1: return "WebSocket instance not found.";
                case -2: return "WebSocket is already connected or in connecting state.";
                case -3: return "WebSocket is not connected.";
                case -4: return "WebSocket is already closing.";
                case -5: return "WebSocket is already closed.";
                case -6: return "WebSocket is not in open state.";
                case -7: return "Cannot close WebSocket. An invalid code was specified or reason is too long.";
                default: return $"Unknown error code {errorCode}.";
            }
        }

        [System.Diagnostics.Conditional("UNITY_WEB_SOCKET_LOG")]
        static void Log(string msg)
        {
            UnityEngine.Debug.Log($"[UnityWebSocket]" +
                $"[{DateTime.Now.TimeOfDay}]" +
                $" {msg}");
        }
    }
}
#endif

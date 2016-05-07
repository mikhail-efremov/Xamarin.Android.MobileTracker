using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Xamarin.Android.MobileTracker
{
    public delegate void OnAckReceive(int messageId);

    public class UdpServer
    {
        public OnAckReceive OnAckReceive;

        private readonly UdpClient _udpClient;

        public UdpServer(string host, int port)
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Connect(host, port);
        }

        public void Send(string message)
        {
            Task.Factory.StartNew(() =>
            {
                var remoteEp = new IPEndPoint(0, 0);
                _udpClient.Send(Encoding.UTF8.GetBytes(message), Encoding.UTF8.GetBytes(message).Length);
                var ack = _udpClient.Receive(ref remoteEp);
                var nack = Encoding.UTF8.GetString(ack).Split(new[] { ':' }, StringSplitOptions.None)[1].Replace("$", string.Empty);
                OnAckReceive(Convert.ToInt32(nack));
            });
        }
    }
}
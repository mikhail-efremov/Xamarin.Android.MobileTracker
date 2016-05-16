using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    public delegate void OnAckReceive(int messageId);

    public class UdpServer
    {
        public OnAckReceive OnAckReceive;

        private readonly UdpClient _udpClient;
        private readonly string _host;
        private readonly int _port;
        private List<Point> _messageBuffer;
        private Timer _timer;

        public int TimeIntervalInMilliseconds = 9000;

        public UdpServer(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
            _messageBuffer = new List<Point>();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Connect(_host, _port);

            _timer = new Timer(OnTimerCall, null, TimeIntervalInMilliseconds, Timeout.Infinite);
        }

        public void Add(Point point)
        {
            lock (_messageBuffer)
            {
                _messageBuffer.Add(point);
            }
        }

        public void Ack(Point point)
        {
            lock (_messageBuffer)
            {
                _messageBuffer.RemoveAll(p => p.Ack == point.Ack);
            }
            OnTimerCall(null);
        }
        
        private void OnTimerCall(object state)
        {
            _timer.Change(TimeIntervalInMilliseconds, Timeout.Infinite);
            if (_messageBuffer.Count > 0)
            {
                var task = Task.Run(() => Send(_messageBuffer[0].GetMessageToSend()));
                if (task.Wait(TimeSpan.FromSeconds(10)))
                    OnAckReceive(task.Result);
                else
                    Console.WriteLine("((");
            }
        }

        private int Send(string message)
        {
            var remoteEp = new IPEndPoint(0, 0);
            _udpClient.Connect(_host, _port);
            _udpClient.Send(Encoding.UTF8.GetBytes(message), Encoding.UTF8.GetBytes(message).Length);
            var ack = _udpClient.Receive(ref remoteEp);
            var nack = Encoding.UTF8.GetString(ack).Split(new[] { ':' }, StringSplitOptions.None)[1].Replace("$", string.Empty);
            return Convert.ToInt32(nack);
        }
    }
}
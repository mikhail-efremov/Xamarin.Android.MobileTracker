using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    public delegate void OnAckReceive(int messageId);
    public delegate void OnRecivePacket(IPEndPoint sender, byte[] packet);

    public class UdpServer
    {
        public OnAckReceive OnAckReceive;

        private readonly UdpClient _udpClient;
        private readonly string _host;
        private readonly int _port;
        private List<Point> _messageBuffer;
        private Timer _timer;

        public int TimeIntervalInMilliseconds = 60000;

        public UdpServer(string host, int port)
        {
            _host = host;
            _port = port;
            _udpClient = new UdpClient();
            _messageBuffer = new List<Point>();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Connect(_host, _port);

            _timer = new Timer(OnTimerCall, null, TimeIntervalInMilliseconds, Timeout.Infinite);

            UdpListener.OnRecivePacket += (sender, packet) =>
            {
                var str = Encoding.ASCII.GetString(packet);
                var nack = str.Split(new[] { ':' }, StringSplitOptions.None)[1].Replace("$", string.Empty);
                OnAckReceive(Convert.ToInt32(nack));
            };
            UdpListener.Start(_udpClient);
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
                var item = _messageBuffer.Single(p => p.Ack == point.Ack);
                _messageBuffer.Remove(item);
            }
            OnTimerCall(null);
        }
        
        private void OnTimerCall(object state)
        {
            _timer.Change(TimeIntervalInMilliseconds, Timeout.Infinite);
            if (_messageBuffer.Count > 0)
            {
                // ReSharper disable once InconsistentlySynchronizedField
                var task = Task.Run(() => Send(_messageBuffer[0].GetMessageToSend()));
               // if (task.Wait(TimeSpan.FromMilliseconds(TimeIntervalInMilliseconds)))
                //    OnAckReceive(task.Result);
            }
        }

        private int Send(string message)
        {
            var remoteEp = new IPEndPoint(0, 0);
            _udpClient.Connect(_host, _port);
            _udpClient.Send(Encoding.UTF8.GetBytes(message), Encoding.UTF8.GetBytes(message).Length);
            return 1;
        }
    }

    public enum WorkStatus
    {
        Pause, Start, Stop, Init, Destroy
    }

    public class UdpListener
    {
        public static OnRecivePacket OnRecivePacket;
        private static UdpClient _client;
        private static WorkStatus _status;
        private static int _port;
        private static IPEndPoint ipep;
        private static EventWaitHandle _socketListenerEvent;
        private static Thread _socketListener;

        private static DateTime _lastReceivedPacket;
        private static ulong _totalReceivedBytes;

        public static bool Start(UdpClient udpClient)
        {
            ipep = new IPEndPoint(IPAddress.Any, _port);
            _socketListenerEvent = new ManualResetEvent(false);
            _status = WorkStatus.Start;
            _client = udpClient;
            //create listening thread
            _socketListenerEvent.Reset();
            _socketListener = new Thread(Listen);
            _socketListener.Start();
            return true;
        }

        private static void Listen()
        {
            while (!_socketListenerEvent.WaitOne(1))
            {
                if (_socketListenerEvent.WaitOne(1))
                    break;
                if (_status == WorkStatus.Stop || _status == WorkStatus.Destroy)
                    break;
                IPEndPoint sender = null;
                byte[] packet = null;
                if (_client == null || _client.Client == null)
                {
                    _client = new UdpClient(ipep);
                }
                lock (_client)
                {
                    packet = _client.Receive(ref sender);
                }

                _lastReceivedPacket = DateTime.UtcNow;
                _totalReceivedBytes += (ulong)packet.Length;

                if (packet.Length == 0)
                {
                    continue;
                }

            if (OnRecivePacket != null)
                OnRecivePacket(sender, packet);
            }
        }

        public static void Stop()
        {
            if (_socketListenerEvent == null) return;
            if (_socketListenerEvent.WaitOne(0)) return;
            if (_socketListenerEvent != null)
                _socketListenerEvent.Set();

            if (_socketListener != null)
                _socketListener.Join(3000);
            _socketListener = null;
            _status = WorkStatus.Stop;
            _client.Close();
        }
    }
}
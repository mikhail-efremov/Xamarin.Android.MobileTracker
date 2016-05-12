using System;
using System.IO;
using System.Linq;
using System.Threading;
using Android.Locations;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class LogicManager
    {
        public OnLocationChanged OnLocationChangedEvent;
        public OnError OnError;

        private LocationListener _locationListener;
        private readonly Configuration _configuration;
        private static bool _isSubscribed = false;
        private readonly UdpServer _udpServer;
        private readonly string _uniqueId;

        public LogicManager(string uniqueId)
        {
            _configuration = new Configuration();
            _uniqueId = uniqueId;

            _udpServer = new UdpServer("216.187.77.151", 6066);
            _udpServer.OnAckReceive += ack =>
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
                var db = new SQLiteConnection(dbPath);
                var points = db.Table<Point>();

                var point = db.Get<Point>(p => p.Ack == ack);
                point.Acked = true;
                db.Update(point);
                _udpServer.Acked(point);
            };
        }

        public void ForceRequestLocation(LocationManager locationManager)
        {
            if (_isSubscribed)
            {
                _locationListener?.SingleRequestLocation();
            }
            else
            {
                _locationListener = new LocationListener(locationManager);
                _locationListener.OnLocationChangedEvent += OnLocationChanged;
                _locationListener?.SingleRequestLocation();
                _isSubscribed = true;
            }
        }

        public void ForceRequestLocation()
        {
            if (_isSubscribed)
            {
                _locationListener?.SingleRequestLocation();
            }
        }

        public void StopRequestLocation()
        {
            if (_locationListener == null) return;
            // ReSharper disable once DelegateSubtraction
            _locationListener.OnLocationChangedEvent -= OnLocationChanged;
            _locationListener.Stop();
        }

        public void OnLocationChanged(Location location)
        {
            if (location == null)
            {
                Console.WriteLine("Unable to determine your location. Try again in a short while.");
            }
            else
            {
                OnLocationChangedEvent(location);
                var point = new Point(_uniqueId, location);
                SaveInBase(point);
                SendToServer(point);
            }
        }

        private void SaveInBase(Point point)
        {
            point.SaveInBase();
        }

        private void SendToServer(Point point)
        {
            _udpServer.Add(point);
        }

        public void InitializeSendProcess()
        {
            try
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
                var db = new SQLiteConnection(dbPath);
                
                var points = db.Table<Point>().Where(p => p.Acked == false);
                if (points.ToList().Count == 0)
                    return;
                foreach (var p in points)
                {
                    _udpServer.Add(p);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
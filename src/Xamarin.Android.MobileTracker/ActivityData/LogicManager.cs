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

                var point = db.Get<Point>(p => p.Ack == ack);
                point.Acked = true;
                db.Update(point);
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
                point.SaveInBase();
                _udpServer.Send(point.Message);
            }
        }

        public void SendOldPoints()
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
                    _udpServer.Send(p.Message);
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
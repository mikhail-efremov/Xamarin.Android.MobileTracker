using System;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Threading;
using Android.Locations;
using Android.Widget;
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

        private const int Angle = 30;
        private const int Distanse = 100;

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

        private Point prevPoint = null;
        private Point prevPrevPoint = null;
        private bool NeedToSendAngle(Point point)
        {
            if (prevPrevPoint == null)
            {
                prevPrevPoint = point;
                return true;
            }

            if(prevPoint == null)
            {
                prevPoint = point;
                return false;
            }

            var x1 = prevPrevPoint.Latitude;
            var y1 = prevPrevPoint.Longitude;
            var x2 = prevPoint.Latitude;
            var y2 = prevPoint.Longitude;

            var x3 = x2;
            var y3 = x3;
            var x4 = point.Latitude;
            var y4 = point.Longitude;
            
            var angle = RadianToDegree(Math.Atan2(y2 - y1, x2 - x1) - Math.Atan2(y4 - y3, x4 - x3));

            var sCoord = new GeoCoordinate(x1, y1);
            var eCoord = new GeoCoordinate(x4, y4);

            var distanse = sCoord.GetDistanceTo(eCoord);

            prevPrevPoint = prevPoint;
            prevPrevPoint = point;
            if (angle > Angle && distanse > Distanse)
                return true;
            return false;
        }

        private double RadianToDegree(double radian)
        {
            var degree = radian * (180.0 / Math.PI);
            if (degree < 0)
                degree = 360 + degree;

            return degree;
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
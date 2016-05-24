using System;
using System.Device.Location;
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
        public static readonly int TimerWait = 60000;

        private LocationListener _locationListener;
        private readonly Configuration _configuration;
        private static bool _isSubscribed;
        private readonly UdpServer _udpServer;
        private readonly string _uniqueId;
        private readonly LocationManager _locationManager;
        public DateTime LastLocationCall;
        public bool IsRequestSendeed;

        private const int Angle = 30;
        private const int Distanse = 100;

        private double StepTimeOutMinutes = 5.0;
        private double TimerTimeOutHour = 1.0;
        //        private readonly Timer _timer;

        public int TimeIntervalInMilliseconds = 3600000;

        public LogicManager(string uniqueId, LocationManager locationManager)
        {
            IsRequestSendeed = false;
            _isSubscribed = false;

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
                _udpServer.Ack(point);
            };
            
            _locationManager = locationManager;

//            _timer = new Timer(OnTimerCall, null, TimeIntervalInMilliseconds, Timeout.Infinite);

            var sensorListener = new SensorListener();
            sensorListener.OnSensorChangedEvent += OnSensorChangedEvent;
        }
/*
        private void OnTimerCall(object state)
        {
            _timer.Change(TimeIntervalInMilliseconds, Timeout.Infinite);
            GetLocation(LocationCallReason.Timer);
        }
*/
        public void ForceRequestLocation(LocationManager locationManager)
        {
            IsRequestSendeed = true;

            if (_isSubscribed)
            {
                _locationListener?.SingleRequestLocation();
            }
            else
            {
                _isSubscribed = true;
                _locationListener = new LocationListener(locationManager);
                _locationListener.OnLocationChangedEvent += OnLocationChanged;
                _locationListener?.SingleRequestLocation();
            }
        }

        public void ForceRequestLocation()
        {
            if (_isSubscribed)
            {
                LastLocationCall = DateTime.Now;
                IsRequestSendeed = true;
                _locationListener?.SingleRequestLocation();
            }
        }

        private void OnSensorChangedEvent()
        {
            GetLocation(LocationCallReason.Step);
        }

        private void GetLocation(LocationCallReason reason)
        {
            if (_locationManager == null)
                return;
            try
            {
                if (IsRequestSendeed)
                {
                    return;
                }
                switch (reason)
                {
                    case LocationCallReason.Step:
                        {
                            if (LastLocationCall < DateTime.Now.AddMinutes(-StepTimeOutMinutes))
                            {
                                ForceRequestLocation(_locationManager);
                            }
                            break;
                        }
                    case LocationCallReason.Angle:
                        {
                            ForceRequestLocation(_locationManager);
                            break;
                        }
                    case LocationCallReason.Timer:
                        {
                            if (LastLocationCall < DateTime.Now.AddHours(TimerTimeOutHour))
                            {
                                ForceRequestLocation(_locationManager);
                            }
                            break;
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
                        }
                }
            }
            catch (Exception e)
            {
                OnError(e);
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
            LastLocationCall = DateTime.Now;
            IsRequestSendeed = false;
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

        private Point _prevPoint = null;
        private Point _prevPrevPoint = null;
        private bool NeedToSendAngle(Point point)
        {
            if (_prevPrevPoint == null)
            {
                _prevPrevPoint = point;
                return true;
            }

            if(_prevPoint == null)
            {
                _prevPoint = point;
                return false;
            }

            var x1 = _prevPrevPoint.Latitude;
            var y1 = _prevPrevPoint.Longitude;
            var x2 = _prevPoint.Latitude;
            var y2 = _prevPoint.Longitude;

            var x3 = x2;
            var y3 = x3;
            var x4 = point.Latitude;
            var y4 = point.Longitude;
            
            var angle = RadianToDegree(Math.Atan2(y2 - y1, x2 - x1) - Math.Atan2(y4 - y3, x4 - x3));

            var sCoord = new GeoCoordinate(x1, y1);
            var eCoord = new GeoCoordinate(x4, y4);

            var distanse = sCoord.GetDistanceTo(eCoord);

            _prevPrevPoint = _prevPoint;
            _prevPrevPoint = point;
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
                db.CreateTable<Point>();
                   
                var points = db.Table<Point>().Where(p => p.Acked == false);
                if (points.ToList().Count == 0)
                    return;
                foreach (var p in points)
                {
                    _udpServer.Add(p);
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }
    }
}
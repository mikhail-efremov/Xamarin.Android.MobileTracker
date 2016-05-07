using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using Xamarin.Android.MobileTracker.ActivityData;
using Android.Locations;
using SQLite;
using Environment = System.Environment;

namespace Xamarin.Android.MobileTracker
{
    [Service]
    public class LocationService : Service
    {
        public static readonly int TimerWait = 50000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        
        public bool IsStarted { get; private set; }
        public Timer Timer { get; private set; }
        public LogicManager LogicManager;
        private const string UniqueId = "868498018462694";
        private Location _currentLocation;
        private string _errorText;
        private LocationManager _locationManager;
        private DemoServiceBinder _binder;
        private UdpServer udpServer;

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsStarted = true;
            
            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);
            Timer = new Timer(o => LogicManager.ForceRequestLocation(_locationManager), null, 0, TimerWait);

            return StartCommandResult.Sticky;
        }

        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager();
            LogicManager.OnLocationChangedEvent += OnLocationChanged;

            udpServer = new UdpServer("216.187.77.151", 6066);
            udpServer.OnAckReceive += ack =>
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
                var db = new SQLiteConnection(dbPath);

                var point = db.Get<Point>(p => p.Ack == ack);
                point.Acked = true;
                db.Update(point);          
            };

            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }
        
        private void OnLocationChanged(Location location)
        {
            try
            {
                if (location == null)
                {
                    Console.WriteLine("Unable to determine your location. Try again in a short while.");
                }
                else
                {
                    _currentLocation = location;
                    
                    var now = DateTime.Now;
                    var year = now.Year.ToString("0000");
                    var month = now.Month.ToString("00");
                    var day = now.Day.ToString("00");
                    var hour = now.Hour.ToString("00");
                    var minute = now.Minute.ToString("00");
                    var second = now.Second.ToString("00");

                    var stringTime = year + month + day + hour + minute + second;
                    var speed = _currentLocation.Speed.ToString(CultureInfo.InvariantCulture);
                    var battery = new Battery();
                    var batteryPest = battery.RemainingChargePercent.ToString();
                    var ack = Point.GetGreatestAck();

                    var xirgo = "+RESP:GTCTN,110107," + UniqueId + ",GL505,0,1,1,8.6," + batteryPest + ",4," + speed +
                                ",0,1111.5,"
                                + CommaToDot(_currentLocation.Longitude.ToString(CultureInfo.InvariantCulture)) + ","
                                + CommaToDot(_currentLocation.Latitude.ToString(CultureInfo.InvariantCulture)) +
                                "," + stringTime + ",0302,0720,2710,E601,,,,20160504114928," + ack + "$";

                    udpServer.Send(xirgo);
                }
            }
            catch (Exception e)
            {
                _errorText = e.Message;
            }
        }

        private string CommaToDot(string message)
        {
            return message.Replace(",", ".");
        }

        public override void OnDestroy()
        {
            LogicManager.StopRequestLocation();
            SendToast("Service was destroyed");
            base.OnDestroy();

            Timer.Dispose();
            Timer = null;

            Log.Debug(Tag, "LocationService destroyed at {0}.", DateTime.UtcNow);
        }

        public override IBinder OnBind(Intent intent)
        {
            _binder = new DemoServiceBinder(this);
            return _binder;
        }

        public void SendToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }

        public class DemoServiceBinder : Binder
        {
            readonly LocationService _service;

            public DemoServiceBinder(LocationService service)
            {
                _service = service;
            }

            public LocationService GetDemoService()
            {
                return _service;
            }
        }
    }
}
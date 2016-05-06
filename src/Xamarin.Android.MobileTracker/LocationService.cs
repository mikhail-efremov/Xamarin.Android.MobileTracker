using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using Xamarin.Android.MobileTracker.ActivityData;
using Android.Locations;

namespace Xamarin.Android.MobileTracker
{
    [Service]
    public class LocationService : Service
    {
        public static readonly int TimerWait = 30000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;

        public bool IsStarted { get; private set; }
        public Timer Timer { get; private set; }
        public LogicManager LogicManager;
        private Location _currentLocation;
        private string _errorText;
        private LocationManager _locationManager;

        DemoServiceBinder binder;

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsStarted = true;
            
            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);
            Timer = new Timer(o =>
            {
                LogicManager.ForceRequestLocation(_locationManager);
            },
                               null,
                               0,
                               TimerWait);

         //   LogicManager.StartRequestLocation(_locationManager);

            return StartCommandResult.Sticky;
        }

        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager();
            LogicManager.OnLocationChangedEvent += OnLocationChanged;

            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }

        private int counter = 0;
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
                    counter++;

                    _currentLocation = location;

                    var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                        ProtocolType.Udp);

                    var serverAddr = IPAddress.Parse("216.187.77.151");
                    var endPoint = new IPEndPoint(serverAddr, 6066);

                    var uniqueId = "868498018462694";

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

                    var xirgo = "+RESP:GTCTN,110107," + uniqueId + ",GL505,0,1,1,8.6," + batteryPest + ",4," + speed +
                                ",0,1111.5,"
                                + CommaToDot(_currentLocation.Longitude.ToString(CultureInfo.InvariantCulture)) + ","
                                + CommaToDot(_currentLocation.Latitude.ToString(CultureInfo.InvariantCulture)) +
                                "," + stringTime + ",0302,0720,2710,E601,,,,20160504114928,1192$";

                    sock.SendTo(Encoding.UTF8.GetBytes(xirgo), endPoint);
                }
            }
            catch (Exception e)
            {
                _errorText = e.Message;
            }
        }

        private string CommaToDot(string message)
        {
            try
            {
                return message.Replace(",", ".");
            }
            catch (Exception e)
            {
                _errorText = e.Message;
            }
            return String.Empty;
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
            binder = new DemoServiceBinder(this);
            return binder;
        }

        public void SendToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }

        public class DemoServiceBinder : Binder
        {
            LocationService service;

            public DemoServiceBinder(LocationService service)
            {
                this.service = service;
            }

            public LocationService GetDemoService()
            {
                return service;
            }
        }
    }
}
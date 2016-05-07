using System;
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
        public static readonly int TimerWait = 60000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        
        public bool IsStarted { get; private set; }
        public Timer Timer { get; private set; }
        public LogicManager LogicManager;
        private Location _currentLocation;
        private string _errorText;
        private LocationManager _locationManager;
        private DemoServiceBinder _binder;

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
            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }
        
        private void OnLocationChanged(Location location)
        {
            try
            {
                if (location != null)
                {
                    _currentLocation = location;
                }
            }
            catch (Exception e)
            {
                _errorText = e.Message;
            }
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
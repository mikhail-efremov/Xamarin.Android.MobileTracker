using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Util;
using Android.Widget;
using Xamarin.Android.MobileTracker.ActivityData;
using Android.Locations;

namespace Xamarin.Android.MobileTracker
{
    [Service]
    public class LocationService : Service, ISensorEventListener
    {
        public static readonly int TimerWait = 60000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        public DateTime LastLocationCall;
        public bool IsRequestSendeed;
        static readonly object _syncLock = new object();

        public bool IsStarted { get; private set; }
        public Timer Timer { get; private set; }
        public LogicManager LogicManager;
        private Location _currentLocation;
        private string _errorText;
        private LocationManager _locationManager;
        private LocationServiceBinder _binder;
        SensorManager _sensorManager;

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsStarted = true;
            IsRequestSendeed = false;

            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);
   /*         Timer = new Timer(o =>
            {
                GetLocation();
            }
            , null, 0, TimerWait);
            */

            return StartCommandResult.Sticky;
        }
        
        public override void OnCreate()
        {
            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _sensorManager.RegisterListener(this,
                                            _sensorManager.GetDefaultSensor(SensorType.StepDetector),
                                            SensorDelay.Ui);
        }

        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager();
            LogicManager.OnLocationChangedEvent += OnLocationChanged;
            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }

        private void GetLocation()
        {
            if (IsRequestSendeed == false && LastLocationCall < DateTime.Now.AddMinutes(-2))
            {
                LogicManager.ForceRequestLocation(_locationManager);
                LastLocationCall = DateTime.Now;
                IsRequestSendeed = true;
            }
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

        public override void OnTaskRemoved(Intent rootIntent)
        {
            base.OnTaskRemoved(rootIntent);
        }

        public override IBinder OnBind(Intent intent)
        {
            _binder = new LocationServiceBinder(this);
            return _binder;
        }

        public void SendToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }

        public class LocationServiceBinder : Binder
        {
            readonly LocationService _service;

            public LocationServiceBinder(LocationService service)
            {
                _service = service;
            }

            public LocationService GetDemoService()
            {
                return _service;
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            //do nothing
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
                GetLocation();
            }
        }
    }
}
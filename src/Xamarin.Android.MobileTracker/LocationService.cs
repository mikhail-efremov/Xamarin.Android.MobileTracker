using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Util;
using Android.Widget;
using Xamarin.Android.MobileTracker.ActivityData;
using Android.Locations;
using Android.Telephony;

namespace Xamarin.Android.MobileTracker
{
    public delegate void OnError(Exception exception);

    [Service]
    public class LocationService : Service, ISensorEventListener
    {
        public static readonly int TimerWait = 60000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        public DateTime LastLocationCall;
        public bool IsRequestSendeed;
        public OnError OnError;
        static readonly object _syncLock = new object();

        public bool IsStarted { get; private set; }
        public LogicManager LogicManager;
        private Location _currentLocation;
        private LocationManager _locationManager;
        private LocationServiceBinder _binder;
        SensorManager _sensorManager;

        public string UniqueId
        {
            get
            {
                var telephonyManager = (TelephonyManager)GetSystemService(TelephonyService);
                return telephonyManager.DeviceId;
            }
        }

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsStarted = true;
            IsRequestSendeed = false;

            SendToast("Service was started 15");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);

            return StartCommandResult.Sticky;
        }
        
        public override void OnCreate()
        {
            try
            {
                _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
                _sensorManager.RegisterListener(this,
                                                _sensorManager.GetDefaultSensor(SensorType.StepCounter),
                                                SensorDelay.Normal);
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager(UniqueId);
            LogicManager.OnError += OnError;
            LogicManager.SendOldPoints();
            LogicManager.OnLocationChangedEvent += OnLocationChanged;
            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }

        private void GetLocation()
        {
            if(_locationManager == null)
                return;
            try
            {
                if (IsRequestSendeed == false && LastLocationCall <  DateTime.Now.AddMinutes(-1.0))
                {
                    LogicManager.ForceRequestLocation(_locationManager);
                    LastLocationCall = DateTime.Now;
                    IsRequestSendeed = true;
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        private void OnLocationChanged(Location location)
        {
            try
            {
                IsRequestSendeed = false;
                if (location != null)
                {
                    _currentLocation = location;
                }
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        public override void OnDestroy()
        {
            try
            {
                LogicManager.StopRequestLocation();
                SendToast("Service was destroyed");
                base.OnDestroy();
                Log.Debug(Tag, "LocationService destroyed at {0}.", DateTime.UtcNow);
            }
            catch (Exception e)
            {
                OnError(e);
            }
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
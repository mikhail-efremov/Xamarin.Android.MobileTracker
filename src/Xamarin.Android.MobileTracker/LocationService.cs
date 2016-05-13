using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
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
    public class LocationService : Service
    {
        public MainActivity Activity;

        public static readonly int TimerWait = 60000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        public DateTime LastLocationCall;
        public bool IsRequestSendeed;
        public OnError OnError;

        public bool IsStarted { get; private set; }
        public LogicManager LogicManager;
        private LocationManager _locationManager;
        private LocationServiceBinder _binder;
        private Timer _timer;
        private SensorListener _sensorListener;

        public string UniqueId
        {
            get
            {
                var telephonyManager = (TelephonyManager)GetSystemService(TelephonyService);
                return telephonyManager.DeviceId;
            }
        }

        public int TimeIntervalInMilliseconds = 3600000;

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            IsStarted = true;
            IsRequestSendeed = false;
            var builder = new Notification.Builder(this)
                .SetContentTitle("Personal Tracker")
                .SetContentText("Service is working. Coming soon to click event!")
                .SetSmallIcon(Resource.Drawable.Icon)
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.IconBlack));

            var notification = builder.Build();

            StartForeground(startId, notification);

            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);

            _timer = new Timer(OnTimerCall, null, TimeIntervalInMilliseconds, Timeout.Infinite);

            _sensorListener = new SensorListener();
            _sensorListener.OnSensorChangedEvent += OnSensorChangedEvent;

            return StartCommandResult.Sticky;
        }

        private void OnSensorChangedEvent()
        {
            GetLocation(LocationCallReason.Step);
        }

        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager(UniqueId);
            LogicManager.OnError += OnError;
            LogicManager.InitializeSendProcess();
            LogicManager.OnLocationChangedEvent += OnLocationChanged;
            _locationManager = (LocationManager)GetSystemService(LocationService);
            SendToast("Service was initialized");
        }

        private void OnTimerCall(object state)
        {
            _timer.Change(TimeIntervalInMilliseconds, Timeout.Infinite);
            GetLocation(LocationCallReason.Timer);
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
                        if (LastLocationCall < DateTime.Now.AddMinutes(-5.0))
                        {
                            LogicManager.ForceRequestLocation(_locationManager);
                            LastLocationCall = DateTime.Now;
                            IsRequestSendeed = true;
                        }
                        break;
                    }
                    case LocationCallReason.Angle:
                    {
                        LogicManager.ForceRequestLocation(_locationManager);
                        LastLocationCall = DateTime.Now;
                        IsRequestSendeed = true;
                        break;
                    }
                    case LocationCallReason.Timer:
                    {
                        if (LastLocationCall < DateTime.Now.AddHours(-1.0))
                        {
                            LogicManager.ForceRequestLocation(_locationManager);
                            LastLocationCall = DateTime.Now;
                            IsRequestSendeed = true;
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

        private void OnLocationChanged(Location location)
        {
            try
            {
                IsRequestSendeed = false;
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
    }

    public enum LocationCallReason
    {
        Angle,
        Step,
        Timer
    }
}
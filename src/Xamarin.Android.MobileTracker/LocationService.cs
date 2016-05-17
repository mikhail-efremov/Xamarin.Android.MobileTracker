using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Telephony;
using Android.Util;
using Android.Widget;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    public delegate void OnError(Exception exception);

    [Service]
    public class LocationService : Service
    {
        public MainActivity Activity;

        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        public OnError OnError;

        public bool IsStarted { get; private set; }
        public LogicManager LogicManager;
        private LocationServiceBinder _binder;

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
            var builder = new Notification.Builder(this)
                .SetContentTitle("Personal Tracker")
                .SetContentText("Service is working. Coming soon to click event!")
                .SetSmallIcon(Resource.Drawable.Icon)
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.IconBlack));

            var notification = builder.Build();

            StartForeground(startId, notification);

            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);

            return StartCommandResult.Sticky;
        }
        
        public void Initialize()
        {
            IsStarted = false;
            LogicManager = new LogicManager(UniqueId, (LocationManager)GetSystemService(LocationService));
            LogicManager.OnError += OnError;
            LogicManager.InitializeSendProcess();
            SendToast("Service was initialized");
        }

        public override void OnDestroy()
        {
            try
            {
                LogicManager.StopRequestLocation();
                SendToast("Service was destroyed");
                base.OnDestroy();
                Log.Debug(Tag, "LocationService destroyed at {0}.", DateTime.UtcNow);
                LogicManager.OnError -= OnError;
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
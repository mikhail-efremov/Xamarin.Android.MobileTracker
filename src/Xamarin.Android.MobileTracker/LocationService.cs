using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Telephony;
using Android.Util;
using Android.Widget;
using SQLite;
using Xamarin.Android.MobileTracker.ActivityData;
using Environment = System.Environment;

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
            LogicManager = new LogicManager(UniqueId, (LocationManager)GetSystemService(LocationService));
            LogicManager.OnError += OnError;
            LogicManager.InitializeSendProcess();
            SendToast("Service was initialized");
        }

        public override void OnDestroy()
        {
            var stat = IsServiceWorked();
            LogDestroy(stat);
            if (stat) return;
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

        private void LogDestroy(bool isWorked)
        {
            var dbPath =
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            db.CreateTable<CrashReport>();
            var destoys = db.Table<CrashReport>();
            db.Insert(new CrashReport
            {
                IsServiceWorked = isWorked 
            });
        }

        public bool IsServiceWorked()
        {
            var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            db.CreateTable<TrackerServiceStatus>();
            var stats = db.Table<TrackerServiceStatus>();
            try
            {
                var state = db.Get<TrackerServiceStatus>(p => p.Id == 1);
                if (state != null)
                {
                    return state.IsServiceWorked;
                }
            }
            catch
            {
                // ignored
            }

            return false;
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
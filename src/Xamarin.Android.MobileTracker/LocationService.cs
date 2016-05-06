using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace Xamarin.Android.MobileTracker
{
    [Service]
    public class LocationService : Service
    {
        public static readonly int TimerWait = 4000;
        private static readonly string Tag = "X:" + typeof(LocationService).Name;
        public Timer Timer { get; private set; }

        [Obsolete("deprecated")]
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            SendToast("Service was started");
            Log.Debug(Tag, "OnStartCommand called at {2}, flags={0}, startid={1}", flags, startId, DateTime.UtcNow);
            Timer = new Timer(o => { Log.Debug(Tag, "Hello from LocationService. {0}", DateTime.UtcNow); },
                               null,
                               0,
                               TimerWait);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            SendToast("Service was destroyed");
            base.OnDestroy();

            Timer.Dispose();
            Timer = null;

            Log.Debug(Tag, "LocationService destroyed at {0}.", DateTime.UtcNow);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public void SendToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }
    }
}
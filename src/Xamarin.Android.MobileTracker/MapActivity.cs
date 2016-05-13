using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Widget;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Android.Locations;
using SQLite;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "MapActivity")]
    public class MapActivity : Activity, ISensorEventListener
    {
        static readonly object _syncLock = new object();
        SensorManager _sensorManager;
        TextView _sensorTextView;
        private TextView _eventTextView;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.MapLayout);

            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _sensorTextView = FindViewById<TextView>(Resource.Id.accelerometer_text);
            _eventTextView = FindViewById<TextView>(Resource.Id.textEvent);

            // Create your application here
        }

        protected override void OnResume()
        {
            base.OnResume();
            _sensorManager.RegisterListener(this,
                                            _sensorManager.GetDefaultSensor(SensorType.Orientation),
                                            SensorDelay.Ui);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // We don't want to do anything here.
        }

        private static int count = 0;
        private static long sumx = 0;
        private static long sumy = 0;
        private static long sumz = 0;

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
                var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "trackerdb.db3");
                var db = new SQLiteConnection(dbPath);
                var points = db.Table<Point>().ToList();

                var x1 = 0;
                var y1 = 0;
                var x2 = 1;
                var y2 = 4;

                var x3 = 1;
                var y3 = 4;
                var x4 = 10;
                var y4 = 9;
                
                //y1=0 x1=1 x2=0 y2=1

                var Angle = RadianToDegree(Math.Atan2(y2 - y1, x2 - x1) - Math.Atan2(y4 - y3, x4 - x3));

                count++;
                //  _sensorTextView.Text = string.Format("x={0:f}, y={1:f}, y={2:f}", e.Values[0], e.Values[1], e.Values[2]);            
                _sensorTextView.Text = string.Format("X:{0} \nY:{1} \nZ:{2}", e.Values[0], e.Values[1], e.Values[2]);

                sumx += (long) e.Values[0];
                sumy += (long) e.Values[1];
                sumz += (long) e.Values[2];

                _eventTextView.Text = "x: " + sumx/count + " \n"
                   + "y: " + sumy/count + " \n"
                   + "z: " + sumz/count + " \n" 
                   + count.ToString();
            }
        }

        public static double RadianToDegree(double radian)
        {
            var degree = radian * (180.0 / Math.PI);
            if (degree < 0)
                degree = 360 + degree;

            return degree;
        }
    }
}
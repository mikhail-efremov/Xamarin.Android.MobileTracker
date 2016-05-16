using Android.App;
using Android.Content;
using Android.Hardware;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public delegate void OnSensorChangedEvent();

    public class SensorListener : Java.Lang.Object, ISensorEventListener
    {
        public OnSensorChangedEvent OnSensorChangedEvent;
        private static readonly object SyncLock = new object();

        public SensorListener()
        {
            var sensorManager = (SensorManager)Application.Context.GetSystemService(Context.SensorService);
            sensorManager.RegisterListener(this,
                                            sensorManager.GetDefaultSensor(SensorType.Accelerometer),
                                            SensorDelay.Normal);
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (SyncLock)
            {
                OnSensorChangedEvent();
            }
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            //do nothing
        }
    }
}
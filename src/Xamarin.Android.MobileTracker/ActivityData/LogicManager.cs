using System;
using Android.Locations;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class LogicManager
    {
        public OnLocationChanged OnLocationChangedEvent;
        private LocationListener _locationListener;
        private Configuration _configuration;
        private static bool isSubscribed = false;

        public LogicManager()
        {
            _configuration = new Configuration();
        }

        public void StartRequestLocation(LocationManager locationManager)
        {
            _locationListener = new LocationListener(locationManager);
            _locationListener.OnLocationChangedEvent += OnLocationChanged;
            _locationListener.RequestLocation(_configuration.MinTime, _configuration.MinDistance);
        }

        public void ForceRequestLocation(LocationManager locationManager)
        {
            if (isSubscribed)
            {
                _locationListener?.SingleRequestLocation();
            }
            else
            {
                _locationListener = new LocationListener(locationManager);
                _locationListener.OnLocationChangedEvent += OnLocationChanged;
                _locationListener?.SingleRequestLocation();
                isSubscribed = true;
            }
        }

        public void ForceRequestLocation()
        {
            if (isSubscribed)
            {
                _locationListener?.SingleRequestLocation();
            }
        }

        public void StopRequestLocation()
        {
            _locationListener.Stop();
        }

        public void OnLocationChanged(Location location)
        {
            if (location == null)
            {
                Console.WriteLine("Unable to determine your location. Try again in a short while.");
            }
            else
            {
                var point = new Point(location);
                point.SaveInBase();
                OnLocationChangedEvent(location);
            }
        }
    }
}
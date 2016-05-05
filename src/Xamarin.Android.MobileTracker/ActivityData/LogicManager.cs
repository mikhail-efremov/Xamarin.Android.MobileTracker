using System;
using System.IO;
using Android.Locations;
using Android.OS;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class LogicManager
    {
        public OnLocationChanged OnLocationChangedEvent;
        private LocationListener _locationListener;
        private Configuration _configuration;

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

        public void ForceRequestLocation()
        {
            _locationListener?.RequestLocation(0, 0);
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
                OnLocationChangedEvent(location);
                var point = new Point(location);
                point.SaveInBase();
            }
        }
    }
}
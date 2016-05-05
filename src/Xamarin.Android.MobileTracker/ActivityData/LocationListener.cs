using System.Linq;
using Android.Locations;
using Android.OS;
using Android.Util;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public delegate void OnLocationChanged(Location location);

    public class LocationListener : Java.Lang.Object, ILocationListener
    {
        public OnLocationChanged OnLocationChangedEvent;
        private readonly LocationManager _locationManager;
        private string _locationProvider;
        private Location _currentLocation;

        public LocationListener(LocationManager locationManager)
        {
            _locationManager = locationManager;
            Initialize();
        }

        private void Initialize()
        {
            var criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            var acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            _locationProvider = acceptableLocationProviders.Any() ? acceptableLocationProviders.First() : string.Empty;
            Log.Debug(MainActivity.Tag, "Using " + _locationProvider + ".");
        }

        /// <summary>
        /// StartRequestLocation
        /// </summary>
        /// <param name="minTime">
        /// <summary>minimum time interval between location updates, in milliseconds</summary></param>
        /// <param name="minDistance">
        /// <summary>minimum distance between location updates, in meters</summary></param>
        public void RequestLocation(long minTime, int minDistance)
        {
            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, minTime, minDistance, this);
        }

        public void SingleRequestLocation()
        {
            _locationManager.RequestSingleUpdate(LocationManager.GpsProvider, this, Looper.MainLooper);
        }

        // removed code for clarity

        public void OnLocationChanged(Location location)
        {
            OnLocationChangedEvent(location);
            _currentLocation = location;
        }

        public void OnProviderDisabled(string provider) { }

        public void OnProviderEnabled(string provider) { }

        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }

        public void Stop()
        {
            _locationManager.RemoveUpdates(this);
        }
    }
}
using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "14 Xamarin.Android.MobileTracker", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILocationListener
    {
        private static readonly string Tag = "X:" + typeof(MainActivity).Name;
        TextView _addressText;
        private TextView _countText;
        Location _currentLocation;
        LocationManager _locationManager;

        public int Count;

        public string LocationProvider;
        public TextView LocationText;
        public TextView InfoText;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            InitializeLocationManager();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            LocationText = FindViewById<TextView>(Resource.Id.location_text);
            InfoText = FindViewById<TextView>(Resource.Id.info_text);
            _countText = FindViewById<TextView>(Resource.Id.count_text);
            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;

            InitializeLocationManager();

            InfoText.Text = new Person().Process().ToString();
        }

        protected override void OnResume()
        {
            base.OnResume();
            Count++;
            _countText.Text = Count.ToString();


            if (_locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            {
             //   _locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, this);
            }
            else
            {
                Log.Info(Tag, "NetworkProvider is not avaible");
            }
            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this);

            if (_currentLocation != null)
                Log.Info(Tag, "Latitude: " + _currentLocation.Latitude);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
        }

        private void InitializeLocationManager()
        {
            _locationManager = (LocationManager)GetSystemService(LocationService);
            var criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            var acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            LocationProvider = acceptableLocationProviders.Any() ? acceptableLocationProviders.First() : string.Empty;
            Log.Debug(Tag, "Using " + LocationProvider + ".");
        }

        private async void AddressButton_OnClick(object sender, EventArgs eventArgs)
        {
            Count++;
            _countText.Text = Count.ToString();

            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this);
            
            _locationManager = (LocationManager)GetSystemService(LocationService);

            _locationManager.GetLastKnownLocation(LocationProvider);


            if (_currentLocation == null)
            {
                _addressText.Text = "Can't determine the current address. Try again in a few minutes.";
                return;
            }

            Address address = await ReverseGeocodeCurrentLocation();
            DisplayAddress(address);
        }

        async Task<Address> ReverseGeocodeCurrentLocation()
        {
            var geocoder = new Geocoder(this);
            var addressList =
                await geocoder.GetFromLocationAsync(_currentLocation.Latitude, _currentLocation.Longitude, 10);

            var address = addressList.FirstOrDefault();
            return address;
        }

        void DisplayAddress(Address address)
        {
            if (address != null)
            {
                var deviceAddress = new StringBuilder();
                for (int i = 0; i < address.MaxAddressLineIndex; i++)
                {
                    deviceAddress.AppendLine(address.GetAddressLine(i));
                }
                // Remove the last comma from the end of the address.
                _addressText.Text = deviceAddress.ToString();
            }
            else
            {
                _addressText.Text = "Unable to determine the address. Try again in a few minutes.";
            }
        }

        // removed code for clarity

        public async void OnLocationChanged(Location location)
        {
            _currentLocation = location;
            if (_currentLocation == null)
            {
                LocationText.Text = "Unable to determine your location. Try again in a short while.";
            }
            else
            {
                LocationText.Text = $"{_currentLocation.Latitude:f6},{_currentLocation.Longitude:f6}";
                var address = await ReverseGeocodeCurrentLocation();
                DisplayAddress(address);
            }
        }

        public void OnProviderDisabled(string provider) { }

        public void OnProviderEnabled(string provider) { }

        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }
    }
}


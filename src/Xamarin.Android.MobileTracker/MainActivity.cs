using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Android.Content;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "14 Xamarin.Android.MobileTracker", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public static readonly string Tag = "X:" + typeof(MainActivity).Name;
        private TextView _addressText;
        private LogicManager _logicManager;
        private TextView _locationText;
        private OnLocationChanged _onLocationChanged;
        private Location _currentLocation;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _logicManager = new LogicManager();
            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);

            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;
            _logicManager.OnLocationChangedEvent += OnLocationChanged;


            Button callHistoryButton = FindViewById<Button>(Resource.Id.CallMapButton);
            callHistoryButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MapActivity));
      //          intent.PutStringArrayListExtra("phone_numbers", phoneNumbers);
                StartActivity(intent);
            };
        }

        protected override void OnResume()
        {
            base.OnResume();
            _logicManager.StartRequestLocation((LocationManager)GetSystemService(LocationService));
        }

        protected override void OnPause()
        {
            base.OnPause();
            _logicManager.StopRequestLocation();
        }

        private async void AddressButton_OnClick(object sender, EventArgs eventArgs)
        {
            if (_currentLocation == null)
            {
                _addressText.Text = "Can't determine the current address. Try again in a few minutes.";
                return;
            }

            var address = await ReverseGeocodeCurrentLocation();
            DisplayAddress(address);
        }

        private async Task<Address> ReverseGeocodeCurrentLocation()
        {
            var geocoder = new Geocoder(this);
            var addressList =
                await geocoder.GetFromLocationAsync(_currentLocation.Latitude, _currentLocation.Longitude, 10);

            var address = addressList.FirstOrDefault();
            return address;
        }

        private void DisplayAddress(Address address)
        {
            if (address != null)
            {
                var deviceAddress = new StringBuilder();
                for (var i = 0; i < address.MaxAddressLineIndex; i++)
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

        private async void OnLocationChanged(Location location)
        {
            if (location == null)
            {
                Console.WriteLine("Unable to determine your location. Try again in a short while.");
            }
            else
            {
                _currentLocation = location;
                _locationText.Text = "Lat:" + _currentLocation.Latitude + " Lon:" + _currentLocation.Longitude;
                var address = await ReverseGeocodeCurrentLocation();
                DisplayAddress(address);
            }
        }
    }
}
﻿using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Locations;
using System.Collections.Generic;
using Android.Util;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "11Xamarin.Android.MobileTracker", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILocationListener
    {
        static readonly string TAG = "X:" + typeof(MainActivity).Name;
        TextView _addressText;
        private TextView _countText;
        Location _currentLocation;
        LocationManager _locationManager;

        public int count = 0;

        string _locationProvider;
        TextView _locationText;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            InitializeLocationManager();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _countText = FindViewById<TextView>(Resource.Id.count_text);
            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;

            InitializeLocationManager();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
        }

        void InitializeLocationManager()
        {
            _locationManager = (LocationManager)GetSystemService(LocationService);
            var criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            var acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
            {
                _locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                _locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + _locationProvider + ".");
        }

        private async void AddressButton_OnClick(object sender, EventArgs eventArgs)
        {
            count++;
            _countText.Text = count.ToString();


            if (_locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                _locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 0, 0, this);
            }
            _locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 0, 0, this);

            if(_currentLocation != null)
            if (_currentLocation.Latitude != 0)
                _countText.Text = _currentLocation.Latitude.ToString();



            _locationManager = (LocationManager)GetSystemService(LocationService);

            _locationManager.GetLastKnownLocation(_locationProvider);

            // DisplayAlert("Alert", "You have been alerted", "OK");

            // You can set the thresholds that suit you here.

            //  _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
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
                _locationText.Text = "Unable to determine your location. Try again in a short while.";
            }
            else
            {
                _locationText.Text = string.Format("{0:f6},{1:f6}", _currentLocation.Latitude, _currentLocation.Longitude);
                Address address = await ReverseGeocodeCurrentLocation();
                DisplayAddress(address);
            }
        }

        public void OnProviderDisabled(string provider) { }

        public void OnProviderEnabled(string provider) { }

        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }
    }
}


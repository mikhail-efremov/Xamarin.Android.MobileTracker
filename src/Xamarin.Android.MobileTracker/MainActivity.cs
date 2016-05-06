using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
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
        private TextView _locationText;
        private TextView _errorText;
        private OnLocationChanged _onLocationChanged;
        private Location _currentLocation;

        LocationService.DemoServiceBinder _binder;
        DemoServiceConnection _serviceConnection;
        private static bool _isBinding = false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _errorText = FindViewById<TextView>(Resource.Id.textErrorInfo);

            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;
            FindViewById<TextView>(Resource.Id.buttonSend).Click += OnSendClick;

            var callHistoryButton = FindViewById<Button>(Resource.Id.CallMapButton);
            callHistoryButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MapActivity));
                StartActivity(intent);
            };

            var buttonStart = FindViewById<Button>(Resource.Id.startService);
            buttonStart.Click += (sender, args) =>
            {
                _serviceConnection = new DemoServiceConnection(this);
                ApplicationContext.BindService(new Intent(this, typeof(LocationService)), _serviceConnection, Bind.AutoCreate);

//                if (_serviceConnection != null)
//                    _binder = _serviceConnection.Binder;
            };

            var buttonStop = FindViewById<Button>(Resource.Id.stopService);
            buttonStop.Click += (sender, args) =>
            {
                ApplicationContext.StopService(new Intent(this, typeof (LocationService)));
                ApplicationContext.UnbindService(_serviceConnection);
                StopService(new Intent(this, typeof(LocationService)));
            };
            
            // restore from connection there was a configuration change, such as a device rotation
#pragma warning disable 618
            _serviceConnection = LastNonConfigurationInstance as DemoServiceConnection;
#pragma warning restore 618
        }

        /*
        // return the service connection if there is a configuration change
        [Obsolete("deprecated")]//??
        public override Java.Lang.Object OnRetainNonConfigurationInstance()
        {
            base.OnRetainNonConfigurationInstance();
            
            return ServiceConnection;
        }
        */

        public void Subscribe()
        {
            if (!_binder.GetDemoService().IsStarted)
                _binder.GetDemoService().StartService(new Intent(this, typeof(LocationService)));
            if (!_isBinding)
            {
                _isBinding = true;
                _binder.GetDemoService().LogicManager.OnLocationChangedEvent += OnLocationChanged;
            }
        }

        private void OnSendClick(object sender, EventArgs eventArgs)
        {
            try
            {
                _binder.GetDemoService().LogicManager.ForceRequestLocation();
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        private async void AddressButton_OnClick(object sender, EventArgs eventArgs)
        {
            try
            {
                if (_currentLocation == null)
                {
                    _addressText.Text = "Can't determine the current address. Try again in a few minutes.";
                    return;
                }

                var address = await ReverseGeocodeCurrentLocation();
                DisplayAddress(address);
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        private async Task<Address> ReverseGeocodeCurrentLocation()
        {
            try
            {
                var geocoder = new Geocoder(this);
                var addressList =
                    await geocoder.GetFromLocationAsync(_currentLocation.Latitude, _currentLocation.Longitude, 10);

                var address = addressList.FirstOrDefault();
                return address;
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
            return null;
        }

        private void DisplayAddress(Address address)
        {
            try
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
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        private int _counter;
        private async void OnLocationChanged(Location location)
        {
            _counter++;
            _currentLocation = location;
            _locationText.Text = _counter + "Lat:" + _currentLocation.Latitude + " Lon:" +  _currentLocation.Longitude;
            var address = await ReverseGeocodeCurrentLocation();
            DisplayAddress(address);
        }

        protected override void OnResume()
        {
            try
            {
                base.OnResume();
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        protected override void OnPause()
        {
            try
            {
                base.OnPause();
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        private class DemoServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private MainActivity Activity { get; }

            public LocationService.DemoServiceBinder Binder { get; private set; }

            public DemoServiceConnection(MainActivity activity)
            {
                Activity = activity;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                var demoServiceBinder = service as LocationService.DemoServiceBinder;
                if (demoServiceBinder != null)
                {
                    Binder = (LocationService.DemoServiceBinder)service;
                    Activity._binder = Binder;
                    
                    // keep instance for preservation across configuration changes
                    Binder = (LocationService.DemoServiceBinder)service;
                    Binder.GetDemoService().Initialize();
                    Activity.Subscribe();
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                Binder.Dispose();
            }
        }
    }
}
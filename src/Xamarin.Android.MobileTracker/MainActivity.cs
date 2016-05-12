using System;
using System.Diagnostics;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Android.Content;
using Switch = Android.Widget.Switch;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "Personal Tracker", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public static readonly string Tag = "X:" + typeof(MainActivity).Name;
        private TextView _addressText;
        private TextView _locationText;
        private TextView _errorText;
        private Location _currentLocation;
        
        LocationService.LocationServiceBinder _binder;
        DemoServiceConnection _serviceConnection;
        private static bool _isBinding = false;
        private int exceptionCounter = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _errorText = FindViewById<TextView>(Resource.Id.textErrorInfo);
            
            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;
            FindViewById<TextView>(Resource.Id.buttonSend).Click += OnSendClick;

            var statusText = FindViewById<TextView>(Resource.Id.textServiceStatus);
            var s = FindViewById<Switch>(Resource.Id.switchService);
            s.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e) {
                if (e.IsChecked)
                {
                    statusText.Text = "Service is on";
                    _serviceConnection = new DemoServiceConnection(this);
                    ApplicationContext.BindService(new Intent(this, typeof(LocationService)), _serviceConnection, Bind.AutoCreate);
                }
                else
                {
                    statusText.Text = "Service is off";
                    ApplicationContext.StopService(new Intent(this, typeof(LocationService)));
                    ApplicationContext.UnbindService(_serviceConnection);
                    StopService(new Intent(this, typeof(LocationService)));
                }
            };
            
            var callHistoryButton = FindViewById<Button>(Resource.Id.CallMapButton);
            callHistoryButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MapActivity));
                StartActivity(intent);
            };
        }

        public void Subscribe()
        {
            if (!_binder.GetDemoService().IsStarted)
                _binder.GetDemoService().StartService(new Intent(this, typeof(LocationService)));
            if (!_isBinding)
            {
                _isBinding = true;
                _binder.GetDemoService().LogicManager.OnLocationChangedEvent += OnLocationChanged;
                _binder.GetDemoService().OnError += OnError;
            }
        }

        private void OnError(Exception exception)
        {
            var stackTracae = new StackTrace();
            exceptionCounter ++;
            var errorMessage = exceptionCounter + ")" + stackTracae.GetFrame(1).GetMethod().Name + ": " + exception.Message;

            _errorText.Text = errorMessage;
            Toast.MakeText(this, errorMessage, ToastLength.Long).Show();
        }

        private void OnSendClick(object sender, EventArgs eventArgs)
        {
            try
            {
                _binder.GetDemoService().LogicManager.ForceRequestLocation();
            }
            catch (Exception e)
            {
                OnError(e);
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
                OnError(e);
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
            catch
            {
                // ignored
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
                    _addressText.Text = deviceAddress.ToString();
                }
            }
            catch
            {
                // ignored
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
                OnError(e);
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
                OnError(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        private class DemoServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private MainActivity Activity { get; }

            private LocationService.LocationServiceBinder Binder { get; set; }

            public DemoServiceConnection(MainActivity activity)
            {
                Activity = activity;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                var demoServiceBinder = service as LocationService.LocationServiceBinder;
                if (demoServiceBinder != null)
                {
                    Binder = (LocationService.LocationServiceBinder)service;
                    Activity._binder = Binder;
                    
                    // keep instance for preservation across configuration changes
                    Binder = (LocationService.LocationServiceBinder)service;
                    Binder.GetDemoService().ACTIVITY = Activity;
                    Binder.GetDemoService().OnError += Activity.OnError;
                    Binder.GetDemoService().Initialize();
                    Activity.Subscribe();
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                Binder.GetDemoService().OnError -= Activity.OnError;
                Binder.Dispose();
            }
        }
    }
}
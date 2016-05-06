using System;
using System.Globalization;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        LocationService.DemoServiceBinder binder;
        DemoServiceConnection ServiceConnection;

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
                ServiceConnection = new DemoServiceConnection(this);
                ApplicationContext.BindService(new Intent(this, typeof(LocationService)), ServiceConnection, Bind.AutoCreate);
            //    StartService(new Intent(this, typeof(LocationService)));
            };

            var buttonStop = FindViewById<Button>(Resource.Id.stopService);
            buttonStop.Click += (sender, args) => { StopService(new Intent(this, typeof(LocationService))); };
            
            // restore from connection there was a configuration change, such as a device rotation
            ServiceConnection = LastNonConfigurationInstance as DemoServiceConnection;

            if (ServiceConnection != null)
                binder = ServiceConnection.Binder;
        }

        // return the service connection if there is a configuration change
        [Obsolete("deprecated")]//??
        public override Java.Lang.Object OnRetainNonConfigurationInstance()
        {
            base.OnRetainNonConfigurationInstance();
            
            return ServiceConnection;
        }

        private static bool isBinding = false;
        private void OnSendClick(object sender, EventArgs eventArgs)
        {
            try
            {
                if(!binder.GetDemoService().IsStarted)
                    binder.GetDemoService().StartService(new Intent(this, typeof (LocationService)));
                binder.GetDemoService().SendToast("OGO EBAT`");
                if (!isBinding)
                {
                    isBinding = true;
                    binder.GetDemoService().LogicManager.OnLocationChangedEvent += OnLocationChanged;
                }
   //             binder.GetDemoService().LogicManager.ForceRequestLocation();
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
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

        private int counter = 0;
        private async void OnLocationChanged(Location location)
        {
            counter++;
            _currentLocation = location;
            _locationText.Text = counter + "Lat:" + _currentLocation.Latitude + " Lon:" +  _currentLocation.Longitude;
            var address = await ReverseGeocodeCurrentLocation();
            DisplayAddress(address);
        }

        public void SendNotification(string message, Type openActivityType)
        {
            var nMgr = (NotificationManager)GetSystemService(NotificationService);
            var notification = new Notification(Resource.Drawable.Icon, message);
            var pendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, openActivityType), 0);
            notification.SetLatestEventInfo(this, "Demo Service Notification", message, pendingIntent);
            nMgr.Notify(0, notification);
        }

        public void SendToast(string message)
        {
            Toast.MakeText(this, message, ToastLength.Long).Show();
        }

        class DemoServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private MainActivity Activity { get; }
            LocationService.DemoServiceBinder binder;

            public LocationService.DemoServiceBinder Binder
            {
                get
                {
                    return binder;
                }
            }

            public DemoServiceConnection(MainActivity activity)
            {
                this.Activity = activity;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                var demoServiceBinder = service as LocationService.DemoServiceBinder;
                if (demoServiceBinder != null)
                {
                    var binder = (LocationService.DemoServiceBinder)service;
                    Activity.binder = binder;
                    
                    // keep instance for preservation across configuration changes
                    this.binder = (LocationService.DemoServiceBinder)service;
                    binder.GetDemoService().Initialize();
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                binder.Dispose();
            }
        }
    }
}
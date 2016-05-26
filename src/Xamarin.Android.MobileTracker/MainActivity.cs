using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Widget;
using SQLite;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "Personal Tracker", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : Activity
    {
        public static readonly string Tag = "X:" + typeof(MainActivity).Name;
        public static DateTime SelectedDateTime;
        private TextView _addressText;
        private TextView _locationText;
        private TextView _errorText;
        private Location _currentLocation;

        private LocationService.LocationServiceBinder _binder;
        private LocationServiceConnection _serviceConnection;
        private static bool _isBinding = false;
        private int _exceptionCounter = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _errorText = FindViewById<TextView>(Resource.Id.textErrorInfo);

            SubscribeUi();
        }

        private void SubscribeUi()
        {
            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;
            FindViewById<TextView>(Resource.Id.buttonSend).Click += OnSendClick;

            var s = FindViewById<ToggleButton>(Resource.Id.toggleService);
            s.CheckedChange += delegate (object sender, CompoundButton.CheckedChangeEventArgs e) {
                if (e.IsChecked)
                {
                    UpdateServiceStatusToStart();
                    _serviceConnection = new LocationServiceConnection(this);
                    ApplicationContext.BindService(new Intent(this, typeof(LocationService)), _serviceConnection, Bind.AutoCreate);
                }
                else
                {
                    UpdateServiceStatusToStop();
                    ApplicationContext.StopService(new Intent(this, typeof(LocationService)));
                    ApplicationContext.UnbindService(_serviceConnection);
                    StopService(new Intent(this, typeof(LocationService)));
                }
            };

            var callHistoryButton = FindViewById<Button>(Resource.Id.CallMapButton);
            callHistoryButton.Click += (sender, e) =>
            {
                var frag = DatePickerFragment.NewInstance(delegate (DateTime time)
                {
                    SelectedDateTime = time;
                    var intent = new Intent(this, typeof(MapActivity));

                    StartActivity(intent);
                });
                frag.Show(FragmentManager, DatePickerFragment.TAG);
            };
        }

        private void UpdateServiceStatusToStart()
        {
            var dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            db.CreateTable<TrackerServiceStatus>();
            var stats = db.Table<TrackerServiceStatus>();
            try
            {
                var stat = db.Get<TrackerServiceStatus>(p => p.Id == 1);
                if (stat != null)
                {
                    stat.IsServiceWorked = true;
                    db.Update(stat);
                }
            }
            catch
            {
                var stat = new TrackerServiceStatus
                {
                    Id = 1,
                    IsServiceWorked = true
                };
                db.Insert(stat);
            }
        }

        private void UpdateServiceStatusToStop()
        {
            var dbPath =
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            db.CreateTable<TrackerServiceStatus>();
            var stats = db.Table<TrackerServiceStatus>();
            try
            {
                var stat = db.Get<TrackerServiceStatus>(p => p.Id == 1);
                if (stat != null)
                {
                    stat.IsServiceWorked = false;
                    db.Update(stat);
                }
            }
            catch
            {
                var stat = new TrackerServiceStatus
                {
                    Id = 1,
                    IsServiceWorked = false
                };
                db.Insert(stat);
            }
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
            _exceptionCounter ++;
            var errorMessage = _exceptionCounter + ")" + stackTracae.GetFrame(1).GetMethod().Name + ": " + exception.Message;

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

        private class LocationServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private MainActivity Activity { get; }

            private LocationService.LocationServiceBinder Binder { get; set; }

            public LocationServiceConnection(MainActivity activity)
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
                    Binder.GetDemoService().Activity = Activity;
                    Binder.GetDemoService().OnError += Activity.OnError;
                    Binder.GetDemoService().Initialize(Application.Context);
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


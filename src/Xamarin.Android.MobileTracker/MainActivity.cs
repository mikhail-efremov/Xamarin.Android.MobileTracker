using System;
using System.Globalization;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using Android.Util;
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
        private LogicManager _logicManager;
        private TextView _locationText;
        private TextView _errorText;
        private OnLocationChanged _onLocationChanged;
        private Location _currentLocation;
        private LocationManager _locationManager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _logicManager = new LogicManager();
            _addressText = FindViewById<TextView>(Resource.Id.address_text);
            _locationText = FindViewById<TextView>(Resource.Id.location_text);
            _errorText = FindViewById<TextView>(Resource.Id.textErrorInfo);

            FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;
            FindViewById<TextView>(Resource.Id.buttonSend).Click += OnSendClick;
            _logicManager.OnLocationChangedEvent += OnLocationChanged;
            _locationManager = (LocationManager)GetSystemService(LocationService);

            var callHistoryButton = FindViewById<Button>(Resource.Id.CallMapButton);
            callHistoryButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MapActivity));
                StartActivity(intent);
            };
        }

        private void OnSendClick(object sender, EventArgs eventArgs)
        {
            //port:6066
            //ip:216.187.77.151 (150)
            //var xirgo = "+RESP:GTCTN,110107,868498018462694,GL505,0,1,1,8.6,91,4,110.5,0,1111.5,-114.001178,51.222072,20160504114928,0302,0720,2710,E601,,,,20160504114928,1192$";
            //GTCTN - continuous message
            //110107 - protocol ver
            //868498018462694 - uniqueId
            //GL505 - device name
            //0 - report id
            //1 - report type
            //1 - movement status
            //8.6 - temperature
            //91 - battery percentage
            //4 - gps accuracy
            //110.5 - speed
            //0 - azimuth
            //1111.5 - altitude
            //-114.001178 - longitude
            //51.222072 - latitude
            //20160504114928 - gps UTC time
            //0302 - mcc
            //0720 - mnc
            //2710 - lac
            //E601 - cellId
            //, - reserved
            //, - reserved
            //, - reserved
            //20160504114928 - send time
            //1192 - count num
            //$ - tail character
            try
            {
                _logicManager.ForceRequestLocation();
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
                _logicManager.StartRequestLocation(_locationManager);
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
                _logicManager.StopRequestLocation();
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
            try
            {
                if (location == null)
                {
                    Console.WriteLine("Unable to determine your location. Try again in a short while.");
                }
                else
                {
                    counter++;

                    _currentLocation = location;
                    _locationText.Text = counter + "Lat:" + _currentLocation.Latitude + " Lon:" +
                                         _currentLocation.Longitude;

                    var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                        ProtocolType.Udp);

                    var serverAddr = IPAddress.Parse("216.187.77.151");
                    var endPoint = new IPEndPoint(serverAddr, 6066);
                    var uniqueId = "868498018462694";

                    var now = DateTime.Now;
                    var year = now.Year.ToString("0000");
                    var month = now.Month.ToString("00");
                    var day = now.Day.ToString("00");
                    var hour = now.Hour.ToString("00");
                    var minute = now.Minute.ToString("00");
                    var second = now.Second.ToString("00");

                    var stringTime = year + month + day + hour + minute + second;
                    var speed = _currentLocation.Speed.ToString(CultureInfo.InvariantCulture);
                    var battery = new Battery();
                    var batteryPest = battery.RemainingChargePercent.ToString();

                    var xirgo = "+RESP:GTCTN,110107," + uniqueId + ",GL505,0,1,1,8.6," + batteryPest + ",4," + speed +
                                ",0,1111.5,"
                                + CommaToDot(_currentLocation.Longitude.ToString(CultureInfo.InvariantCulture)) + ","
                                + CommaToDot(_currentLocation.Latitude.ToString(CultureInfo.InvariantCulture)) +
                                "," + stringTime + ",0302,0720,2710,E601,,,,20160504114928,1192$";

                    sock.SendTo(Encoding.UTF8.GetBytes(xirgo), endPoint);

                    var address = await ReverseGeocodeCurrentLocation();
                    DisplayAddress(address);
                }
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
        }

        private string CommaToDot(string message)
        {
            try
            {
                return message.Replace(",", ".");
            }
            catch (Exception e)
            {
                _errorText.Text = e.Message;
            }
            return String.Empty;
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
    }
}
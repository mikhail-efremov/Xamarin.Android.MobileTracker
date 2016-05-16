using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Java.Net;
using Xamarin.Android.MobileTracker;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;
using CustomMap = Xamarin.Android.MobileTracker.CustomMap;

[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace Xamarin.Android.MobileTracker
{
    public class CustomMapRenderer : MapRenderer, GoogleMap.IInfoWindowAdapter, IOnMapReadyCallback
    {
        private GoogleMap _map;
        private List<CustomPin> _customPins;
        private List<Position> _routeCoordinates;
        private bool _isDrawn;

        protected override void OnElementChanged(Forms.Platform.Android.ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                _map.InfoWindowClick -= OnInfoWindowClick;
            }

            if (e.NewElement != null)
            {
                var formsMap = (CustomMap)e.NewElement;
                _customPins = formsMap.CustomPins;
                _routeCoordinates = formsMap.RouteCoordinates;

                ((MapView)Control).GetMapAsync(this);
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _map = googleMap;

            var polylineOptions = new PolylineOptions();
            polylineOptions.InvokeColor(0x66FF0000);

            foreach (var position in _routeCoordinates)
            {
                polylineOptions.Add(new LatLng(position.Latitude, position.Longitude));
            }
            
            _map.AddPolyline(polylineOptions);

            _map.InfoWindowClick += OnInfoWindowClick;
            _map.SetInfoWindowAdapter(this);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName.Equals("VisibleRegion") && !_isDrawn)
            {
                _map.Clear();
                if(_customPins != null)
                foreach (var pin in _customPins)
                {
                    var marker = new MarkerOptions();
                    marker.SetPosition(new LatLng(pin.Pin.Position.Latitude, pin.Pin.Position.Longitude));
                    marker.SetTitle(pin.Pin.Label);
                    marker.SetSnippet(pin.Pin.Address);
                 //   marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.pin));

                    _map.AddMarker(marker);
                }

                var polylineOptions = new PolylineOptions();
                polylineOptions.InvokeColor(0x66FF0000);

                foreach (var position in _routeCoordinates)
                {
                    polylineOptions.Add(new LatLng(position.Latitude, position.Longitude));
                }

                for (var i = 1; i < polylineOptions.Points.Count; i++)
                {
                    DrawArrowHead(_map, polylineOptions.Points[i - 1], polylineOptions.Points[i]);
                }

                _map.AddPolyline(polylineOptions);
                _isDrawn = true;
            }
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            if (changed)
            {
                _isDrawn = false;
            }
        }

        void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
        {
            var customPin = GetCustomPin(e.Marker);
            if (customPin == null)
            {
                throw new Exception("Custom pin not found");
            }
        }

        public global::Android.Views.View GetInfoContents(Marker marker)
        {
            var inflater = global::Android.App.Application.Context.GetSystemService(Context.LayoutInflaterService) as global::Android.Views.LayoutInflater;
            if (inflater != null)
            {
                global::Android.Views.View view;

                var customPin = GetCustomPin(marker);
                if (customPin == null)
                {
                    throw new Exception("Custom pin not found");
                }

                if (customPin.Id == "Xamarin")
                {
                    view = inflater.Inflate(Resource.Layout.XamarinMapInfoWindow, null);
                }
                else
                {
                    view = inflater.Inflate(Resource.Layout.MapInfoWindow, null);
                }

                var infoTitle = view.FindViewById<TextView>(Resource.Id.InfoWindowTitle);
                var infoSubtitle = view.FindViewById<TextView>(Resource.Id.InfoWindowSubtitle);

                if (infoTitle != null)
                {
                    infoTitle.Text = marker.Title;
                }
                if (infoSubtitle != null)
                {
                    infoSubtitle.Text = marker.Snippet;
                }

                return view;
            }
            return null;
        }

        public global::Android.Views.View GetInfoWindow(Marker marker)
        {
            return null;
        }

        CustomPin GetCustomPin(Marker annotation)
        {
            var position = new Position(annotation.Position.Latitude, annotation.Position.Longitude);
            return _customPins.FirstOrDefault(pin => pin.Pin.Position == position);
        }

        private double degreesPerRadian = 180.0 / Math.PI;

        private void DrawArrowHead(GoogleMap mMap, LatLng from, LatLng to)
        {
            // obtain the bearing between the last two points
            double bearing = GetBearing(from, to);

            // round it to a multiple of 3 and cast out 120s
            double adjBearing = Math.Round(bearing / 3) * 3;
            while (adjBearing >= 120)
            {
                adjBearing -= 120;
            }

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);

            // Get the corresponding triangle marker from Google        
            URL url;
            Bitmap image = null;

            try
            {
                url = new URL("http://www.google.com/intl/en_ALL/mapfiles/dir_" + (int)adjBearing + ".png");
                try
                {
                    image = BitmapFactory.DecodeStream(url.OpenConnection().InputStream);
                }
                catch (IOException e)
                {
                }
            }
            catch (MalformedURLException e)
            {
            }

            if (image != null)
            {

                // Anchor is ratio in range [0..1] so value of 0.5 on x and y will center the marker image on the lat/long
                float anchorX = 0.5f;
                float anchorY = 0.5f;

                int offsetX = 0;
                int offsetY = 0;

                // images are 24px x 24px
                // so transformed image will be 48px x 48px

                //315 range -- 22.5 either side of 315
                if (bearing >= 292.5 && bearing < 335.5)
                {
                    offsetX = 24;
                    offsetY = 24;
                }
                //270 range
                else if (bearing >= 247.5 && bearing < 292.5)
                {
                    offsetX = 24;
                    offsetY = 12;
                }
                //225 range
                else if (bearing >= 202.5 && bearing < 247.5)
                {
                    offsetX = 24;
                    offsetY = 0;
                }
                //180 range
                else if (bearing >= 157.5 && bearing < 202.5)
                {
                    offsetX = 12;
                    offsetY = 0;
                }
                //135 range
                else if (bearing >= 112.5 && bearing < 157.5)
                {
                    offsetX = 0;
                    offsetY = 0;
                }
                //90 range
                else if (bearing >= 67.5 && bearing < 112.5)
                {
                    offsetX = 0;
                    offsetY = 12;
                }
                //45 range
                else if (bearing >= 22.5 && bearing < 67.5)
                {
                    offsetX = 0;
                    offsetY = 24;
                }
                //0 range - 335.5 - 22.5
                else
                {
                    offsetX = 12;
                    offsetY = 24;
                }

                Bitmap wideBmp;
                Canvas wideBmpCanvas;
                Rect src, dest;

                // Create larger bitmap 4 times the size of arrow head image
                wideBmp = Bitmap.CreateBitmap(image.Width * 2, image.Height * 2, image.GetConfig());

                wideBmpCanvas = new Canvas(wideBmp);

                src = new Rect(0, 0, image.Width, image.Height);
                dest = new Rect(src);
                dest.Offset(offsetX, offsetY);

                wideBmpCanvas.DrawBitmap(image, src, dest, null);

                mMap.AddMarker(new MarkerOptions()
                .SetPosition(to)
                .SetIcon(BitmapDescriptorFactory.FromBitmap(wideBmp))
                .Anchor(anchorX, anchorY));
            }
        }

        private double GetBearing(LatLng from, LatLng to)
        {
            double lat1 = from.Latitude * Math.PI / 180.0;
            double lon1 = from.Longitude * Math.PI / 180.0;
            double lat2 = to.Latitude * Math.PI / 180.0;
            double lon2 = to.Longitude * Math.PI / 180.0;

            // Compute the angle.
            double angle = -Math.Atan2(Math.Sin(lon1 - lon2) * Math.Cos(lat2), Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

            if (angle < 0.0)
                angle += Math.PI * 2.0;

            // And convert result to degrees.
            angle = angle * degreesPerRadian;

            return angle;
        }
    }
}
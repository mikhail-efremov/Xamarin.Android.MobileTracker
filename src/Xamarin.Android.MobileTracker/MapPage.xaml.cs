using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace Xamarin.Android.MobileTracker
{
	public partial class MapPage : ContentPage
	{
		public MapPage (List<ActivityData.Point> points)
		{
			InitializeComponent ();

            customMap.CustomPins = new List<CustomPin>();

		    foreach (var point in points)
		    {
		        var pin = new CustomPin
		        {
		            Pin = new Pin
		            {
		                Type = PinType.Place,
		                Position = new Position(point.Latitude, point.Longitude),
		                Label = point.GpsTime.ToLongDateString(),
		                Address = "Speed: " + point.Speed
		            },
		            Id = "Xamarin",
		            Url = "http://xamarin.com/about/"
		        };
                customMap.CustomPins.Add(pin);
                customMap.Pins.Add(pin.Pin);
            }

		    customMap.RouteCoordinates = new List<Position>();
		    foreach (var point in points)
		    {
                customMap.RouteCoordinates.Add(new Position(point.Latitude, point.Longitude));
            }
            
            customMap.MoveToRegion (MapSpan.FromCenterAndRadius (customMap.RouteCoordinates[0], Distance.FromMiles (1.0)));
		}
	}
}

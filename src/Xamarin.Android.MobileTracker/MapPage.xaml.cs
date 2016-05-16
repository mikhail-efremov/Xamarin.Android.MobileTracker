using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Point = Xamarin.Android.MobileTracker.ActivityData.Point;

namespace Xamarin.Android.MobileTracker
{
	public partial class MapPage : ContentPage
	{
		public MapPage (IReadOnlyCollection<Point> points)
		{
			InitializeComponent ();

            customMap.CustomPins = new List<CustomPin>();

		    foreach (var pin in points.Select(point => new CustomPin
		    {
		        Pin = new Pin
		        {
		            Type = PinType.Place,
		            Position = new Position(point.Latitude, point.Longitude),
		            Label = point.GpsTime.ToLongDateString(),
		            Address = "Speed: " + point.Speed + " km/h"
		        },
		        Id = "Xamarin",
		        Url = "http://xamarin.com/about/"
		    }))
		    {
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

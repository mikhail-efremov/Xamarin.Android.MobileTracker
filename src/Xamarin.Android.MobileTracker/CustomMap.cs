using System.Collections.Generic;
using Xamarin.Forms.Maps;

namespace Xamarin.Android.MobileTracker
{
	public class CustomMap : Map
	{
		public List<CustomPin> CustomPins { get; set; }
        public List<Position> RouteCoordinates { get; set; }
    }
}

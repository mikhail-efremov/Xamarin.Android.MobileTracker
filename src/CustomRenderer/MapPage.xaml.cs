using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace CustomRenderer
{
	public partial class MapPage : ContentPage
	{
		public MapPage ()
		{
			InitializeComponent ();

            var pin = new CustomPin
            {
                Pin = new Pin
                {
                    Type = PinType.Place,
                    Position = new Position(48.738967, 37.58435),
                    Label = "Xamarin San Francisco Office",
                    Address = "394 Pacific Ave, San Francisco CA"
                },
                Id = "Xamarin",
                Url = "http://xamarin.com/about/"
            };

            customMap.CustomPins = new List<CustomPin> { pin };
			customMap.Pins.Add (pin.Pin);
			customMap.MoveToRegion (MapSpan.FromCenterAndRadius (new Position (48.738967, 37.58435), Distance.FromMiles (1.0)));
		}
	}
}

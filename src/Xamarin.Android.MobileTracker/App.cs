using System.Collections.Generic;
using Xamarin.Forms;

namespace Xamarin.Android.MobileTracker
{
	public class App : Application
	{
		public static double ScreenHeight;
		public static double ScreenWidth;

		public App (List<ActivityData.Point> points)
		{
			MainPage = new MapPage (points);
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}


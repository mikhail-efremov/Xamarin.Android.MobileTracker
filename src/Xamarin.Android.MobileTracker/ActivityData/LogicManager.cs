using System;
using System.IO;
using Android.Locations;
using Android.OS;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class LogicManager
    {
        public void OnLocationChanged(Location location)
        {
            if (location == null)
            {
                Console.WriteLine("Unable to determine your location. Try again in a short while.");
            }
            else
            {
                var point = new Point(location);
                point.SaveInBase();
            }
        }
    }
}
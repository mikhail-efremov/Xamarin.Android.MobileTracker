using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using SQLite;
using Xamarin.Android.MobileTracker.ActivityData;

namespace Xamarin.Android.MobileTracker
{
    [Activity(Label = "PersonalTracker.Map", Icon = "@drawable/icon", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MapActivity : Forms.Platform.Android.FormsApplicationActivity
    {
        public static readonly string Tag = "X:" + typeof(MainActivity).Name;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var time = MainActivity.SelectedDateTime;

            var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            var points = db.Table<Point>();


            var poids = new List<Point>();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var point in points)
            {
                if (point.GpsTime.Date == time.Date)
                    poids.Add(point);
            }

            Forms.Forms.Init(this, bundle);
            FormsMaps.Init(this, bundle);

            var width = Resources.DisplayMetrics.WidthPixels;
            var height = Resources.DisplayMetrics.HeightPixels;
            var density = Resources.DisplayMetrics.Density;

            App.ScreenWidth = (width - 0.5f) / density;
            App.ScreenHeight = (height - 0.5f) / density;


            LoadApplication(new App(poids));
        }
    }
}
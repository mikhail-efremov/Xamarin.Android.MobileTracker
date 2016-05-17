using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
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
            var points = GetPointsByDate(MainActivity.SelectedDateTime);
            if (points == null || points.Count == 0)
            {
                Toast.MakeText(this, "Not find points on this date", ToastLength.Long).Show();
                Finish();
            }

            Forms.Forms.Init(this, bundle);
            FormsMaps.Init(this, bundle);

            var width = Resources.DisplayMetrics.WidthPixels;
            var height = Resources.DisplayMetrics.HeightPixels;
            var density = Resources.DisplayMetrics.Density;

            App.ScreenWidth = (width - 0.5f) / density;
            App.ScreenHeight = (height - 0.5f) / density;
            LoadApplication(new App(points));
        }

        private List<Point> GetPointsByDate(DateTime date)
        {
            var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);
            var points = new List<Point>();
            try
            {
                var queriedPoints = db.Table<Point>();

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var point in queriedPoints)
                {
                    if (point.GpsTime.Date == date.Date)
                        points.Add(point);
                }
            }
            catch
            {
                Toast.MakeText(this, "Not find points on this date", ToastLength.Long).Show();
                Finish();
            }
            return points;
        }
    }
}
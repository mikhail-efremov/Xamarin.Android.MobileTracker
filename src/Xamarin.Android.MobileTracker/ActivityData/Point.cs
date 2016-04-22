using System;
using System.IO;
using Android.Gestures;
using Android.Locations;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class Point
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public float Accuracy { get; set; }
        public double Altitude { get; set; }
        public float Bearing { get; set; }
        public long ElapsedRealtimeNanos { get; set; }
        public bool HasAccuracy { get; set; }
        public bool HasAltitude { get; set; }
        public bool HasBearing { get; set; }
        public bool HasSpeed { get; set; }
        public bool IsFromMockProvider { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Provider { get; set; }
        public float Speed { get; set; }
        public long Time { get; set; }

        public Point(Location location)
        {
            Accuracy = location.Accuracy;
            Altitude = location.Altitude;
            Bearing = location.Bearing;
            ElapsedRealtimeNanos = location.ElapsedRealtimeNanos;
            HasAccuracy = location.HasAccuracy;
            HasAltitude = location.HasAltitude;
            HasBearing = location.HasBearing;
            HasSpeed = location.HasSpeed;
            IsFromMockProvider = location.IsFromMockProvider;
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Provider = location.Provider;
            Speed = location.Speed;
            Time = location.Time;
        }

        public Point()
        { }

        public int SaveInBase()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "database.db3");
            var db = new SQLiteConnection(dbPath);
            
            db.CreateTable<Point>();
            db.Insert(this);

            var point = db.Get<Point>(1);
            var pointList = db.Table<Point>();

            return 1;
        }

        public override string ToString()
        {
            return string.Format("lat:" + Latitude + "lon:" + Longitude);
        }
    }
}
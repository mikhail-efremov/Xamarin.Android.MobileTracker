using System;
using System.Globalization;
using System.IO;
using Android.Database.Sqlite;
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
        public string Message { get; set; }
        public int Ack { get; set; }
        public bool Acked { get; set; }

        public Point(string uniqueId, Location location)
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
            InitMessage(uniqueId, location);
        }

        public Point()
        { }

        public void InitMessage(string uniqueId, Location location)
        {
            var now = DateTime.Now;
            var year = now.Year.ToString("0000");
            var month = now.Month.ToString("00");
            var day = now.Day.ToString("00");
            var hour = now.Hour.ToString("00");
            var minute = now.Minute.ToString("00");
            var second = now.Second.ToString("00");

            var stringTime = year + month + day + hour + minute + second;
            var speed = location.Speed.ToString(CultureInfo.InvariantCulture);
            var battery = new Battery();
            var batteryPest = battery.RemainingChargePercent.ToString();
            Ack = GetGreatestAck();

            Message = "+RESP:GTCTN,110107," + uniqueId + ",GL505,0,1,1,8.6," + batteryPest + ",4," + speed +
                        ",0,1111.5,"
                        + CommaToDot(location.Longitude.ToString(CultureInfo.InvariantCulture)) + ","
                        + CommaToDot(location.Latitude.ToString(CultureInfo.InvariantCulture)) +
                        "," + stringTime + ",0302,0720,2710,E601,,,,20160504114928," + Ack + "$";
        }
        
        private string CommaToDot(string message)
        {
            return message.Replace(",", ".");
        }

        public int SaveInBase()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);

            var das = db.Table<Point>();

            db.CreateTable<Point>();
            db.Insert(this);

            return 1;
        }

        public static int GetGreatestAck()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
            var db = new SQLiteConnection(dbPath);

            db.CreateTable<Point>();

            var greatestAck = 0;

            try
            {
                greatestAck = db.Table<Point>()
                    .OrderByDescending(point => point.Ack).First().Ack;
            }
            catch
            {
                // ignored
            }

            return greatestAck + 1;
        }

        public override string ToString()
        {
            return string.Format("lat:" + Latitude + "lon:" + Longitude);
        }
    }
}
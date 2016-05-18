using System;
using System.Globalization;
using System.IO;
using Android.Locations;
using Java.IO;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class Point : Java.Lang.Object, ISerializable
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
        public int Ack { get; set; }
        public bool Acked { get; set; }
        public DateTime GpsTime { get; set; }
        public string UniqueId { get; set; }

        public Point(string uniqueId, Location location)
        {
            UniqueId = uniqueId;
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
            GpsTime = DateTime.UtcNow;
            Ack = GetGreatestAck();
        }

        public Point()
        { }

        public string GetMessageToSend()
        {
            var now = DateTime.UtcNow;
            var year = now.Year.ToString("0000");
            var month = now.Month.ToString("00");
            var day = now.Day.ToString("00");
            var hour = now.Hour.ToString("00");
            var minute = now.Minute.ToString("00");
            var second = now.Second.ToString("00");
            var sendTime = year + month + day + hour + minute + second;
            
            var gpsyear = GpsTime.Year.ToString("0000");
            var gpsmonth = GpsTime.Month.ToString("00");
            var gpsday = GpsTime.Day.ToString("00");
            var gpshour = GpsTime.Hour.ToString("00");
            var gpsminute = GpsTime.Minute.ToString("00");
            var gpssecond = GpsTime.Second.ToString("00");
            var gpsTime = gpsyear + gpsmonth + gpsday + gpshour + gpsminute + gpssecond;

            var speed = Speed.ToString(CultureInfo.InvariantCulture);
            var battery = new Battery();
            var batteryPest = battery.RemainingChargePercent.ToString();

            return "+RESP:GTCTN,110107," + UniqueId + ",GL505,0,1,1,8.6," + batteryPest + ",4," + speed +
                        ",0,1111.5,"
                        + CommaToDot(Longitude.ToString(CultureInfo.InvariantCulture)) + ","
                        + CommaToDot(Latitude.ToString(CultureInfo.InvariantCulture)) +
                        "," + gpsTime + ",0302,0720,2710,E601,,,," + sendTime + "," + Ack + "$";
            //sended time and gps time is equal. it bad
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
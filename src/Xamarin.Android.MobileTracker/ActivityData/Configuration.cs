using System;
using System.IO;
using SQLite;
using Environment = System.Environment;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    internal class Configuration
    {
        public long MinTime { get; set; }
        public int MinDistance { get; set; }

        public Configuration ()
        {
        }

        public Configuration(long minTime, int minDistance)
        {
            try
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "trackerdb.db3");
                var db = new SQLiteConnection(dbPath);

                db.CreateTable<Configuration>();
                var config = db.Get<Configuration>(0);

                if (config != null)
                {
                    MinTime = config.MinTime;
                    MinDistance = config.MinDistance;
                }
                else
                {
                    db.DeleteAll<Configuration>();
                    MinTime = minTime;
                    MinDistance = minDistance;
                    db.Insert(this);
                }
            }
            catch (Exception)
            {
                MinTime = minTime;
                MinDistance = minDistance;
            }
        }
    }
}
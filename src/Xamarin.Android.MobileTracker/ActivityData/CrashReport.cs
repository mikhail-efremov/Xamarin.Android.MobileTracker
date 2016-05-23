using System;
using Java.IO;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class CrashReport : Java.Lang.Object, ISerializable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime CrashTime { get; set; }
        public bool IsServiceWorked;

        public CrashReport()
        {
            CrashTime = DateTime.Now;
        }
    }
}
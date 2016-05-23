using Java.IO;
using SQLite;

namespace Xamarin.Android.MobileTracker.ActivityData
{
    public class TrackerServiceStatus : Java.Lang.Object, ISerializable
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public bool IsServiceWorked { get; set; }

        public TrackerServiceStatus()
        {
            IsServiceWorked = false;
        }
    }
}
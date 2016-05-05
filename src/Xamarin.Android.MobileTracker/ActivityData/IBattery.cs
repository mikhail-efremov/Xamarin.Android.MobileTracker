namespace Xamarin.Android.MobileTracker.ActivityData
{
    public enum BatteryStatus
    {
        Charging,
        Discharging,
        Full,
        NotCharging,
        Unknown
    }

    public enum PowerSource
    {
        Battery,
        Ac,
        Usb,
        Wireless,
        Other
    }

    public interface IBattery
    {
        int RemainingChargePercent { get; }
        global::Android.OS.BatteryStatus Status { get; }
        PowerSource PowerSource { get; }
    }
}
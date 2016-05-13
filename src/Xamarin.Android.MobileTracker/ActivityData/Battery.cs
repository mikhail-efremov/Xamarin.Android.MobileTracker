using System;
using Android.App;
using Android.Content;
using Android.OS;

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

    public class Battery : IBattery
    {
        public int RemainingChargePercent
        {
            get
            {
                try
                {
                    using (var filter = new IntentFilter(Intent.ActionBatteryChanged))
                    {
                        using (var battery = Application.Context.RegisterReceiver(null, filter))
                        {
                            var level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
                            var scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);

                            return (int)Math.Floor(level * 100D / scale);
                        }
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Ensure you have android.permission.BATTERY_STATS");
                    throw;
                }

            }
        }

        public global::Android.OS.BatteryStatus Status
        {
            get
            {
                try
                {
                    using (var filter = new IntentFilter(Intent.ActionBatteryChanged))
                    {
                        using (var battery = Application.Context.RegisterReceiver(null, filter))
                        {
                            int status = battery.GetIntExtra(BatteryManager.ExtraStatus, -1);
                            var isCharging = status == (int)global::Android.OS.BatteryStatus.Charging || status == (int)global::Android.OS.BatteryStatus.Full;

                            var chargePlug = battery.GetIntExtra(BatteryManager.ExtraPlugged, -1);
                            var usbCharge = chargePlug == (int)BatteryPlugged.Usb;
                            var acCharge = chargePlug == (int)BatteryPlugged.Ac;
                            bool wirelessCharge = false;
                            wirelessCharge = chargePlug == (int)BatteryPlugged.Wireless;

                            isCharging = (usbCharge || acCharge || wirelessCharge);
                            if (isCharging)
                                return global::Android.OS.BatteryStatus.Charging;

                            switch (status)
                            {
                                case (int)global::Android.OS.BatteryStatus.Charging:
                                    return global::Android.OS.BatteryStatus.Charging;
                                case (int)global::Android.OS.BatteryStatus.Discharging:
                                    return global::Android.OS.BatteryStatus.Discharging;
                                case (int)global::Android.OS.BatteryStatus.Full:
                                    return global::Android.OS.BatteryStatus.Full;
                                case (int)global::Android.OS.BatteryStatus.NotCharging:
                                    return global::Android.OS.BatteryStatus.NotCharging;
                                default:
                                    return global::Android.OS.BatteryStatus.Unknown;
                            }
                        }
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Ensure you have android.permission.BATTERY_STATS");
                    throw;
                }
            }
        }

        public PowerSource PowerSource
        {
            get
            {
                try
                {
                    using (var filter = new IntentFilter(Intent.ActionBatteryChanged))
                    {
                        using (var battery = Application.Context.RegisterReceiver(null, filter))
                        {
                            int status = battery.GetIntExtra(BatteryManager.ExtraStatus, -1);
                            var isCharging = status == (int)global::Android.OS.BatteryStatus.Charging || status == (int)global::Android.OS.BatteryStatus.Full;

                            var chargePlug = battery.GetIntExtra(BatteryManager.ExtraPlugged, -1);
                            var usbCharge = chargePlug == (int)BatteryPlugged.Usb;
                            var acCharge = chargePlug == (int)BatteryPlugged.Ac;

                            bool wirelessCharge = false;
                            wirelessCharge = chargePlug == (int)BatteryPlugged.Wireless;

                            isCharging = (usbCharge || acCharge || wirelessCharge);

                            if (!isCharging)
                                return PowerSource.Battery;
                            else if (usbCharge)
                                return PowerSource.Usb;
                            else if (acCharge)
                                return PowerSource.Ac;
                            else if (wirelessCharge)
                                return PowerSource.Wireless;
                            else
                                return PowerSource.Other;
                        }
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Ensure you have android.permission.BATTERY_STATS");
                    throw;
                }
            }
        }
    }
}
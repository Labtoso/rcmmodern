using System;
using System.Management;

namespace RcmGui
{
    internal enum RcmState { NotFound, NoDriver, Ready }

    /// <summary>Erkennt eine Switch im RCM-Modus (APX, USB VID 0955 / PID 7321) per WMI.</summary>
    internal static class RcmDevice
    {
        private const string Query =
            "SELECT Service FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB\\\\VID[_]0955&PID[_]7321%'";

        public static RcmState Detect()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(Query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject dev in results)
                    {
                        using (dev)
                        {
                            var service = dev["Service"] as string;
                            return string.Equals(service, "libusbK", StringComparison.OrdinalIgnoreCase)
                                ? RcmState.Ready
                                : RcmState.NoDriver;
                        }
                    }
                }
            }
            catch (ManagementException) { }
            return RcmState.NotFound;
        }
    }
}

using Windows.System.Profile;

namespace Sora.Device
{
    public class VersionInfo
    {
        public static string GetInfo()
        {
            var info = AnalyticsInfo.VersionInfo;

            var family = info.DeviceFamily;
            var rawVersion = info.DeviceFamilyVersion;

            ulong v = ulong.Parse(rawVersion);
            ulong v1 = (v & 0xFFFF000000000000L) >> 48;
            ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
            ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
            ulong v4 = (v & 0x000000000000FFFFL);

            var ver = $"{v1}.{v2}.{v3}.{v4}";

            return $"DeviceFamily: {family}, OSVersion: {ver}";
                
        }

    }
}

using System;

namespace SystrayExtension
{
    public interface IReadAndPersistSettings
    {
        void SetAltcWidth(int width);
        int GetAltcWidth();
        void SetAltArrowWidth(int width);
        int GetAltArrowWidth();
        void SetCtrlArrowWidth(int width);
        int GetCtrlArrowWidth();
    }

    public class ReadAndPersistSettings : IReadAndPersistSettings
    {
        readonly /* static */ Windows.Storage.ApplicationDataContainer localSettings;// = Windows.Storage.ApplicationData.Current.LocalSettings;
        readonly /* static */ Windows.Storage.StorageFolder localFolder;// = Windows.Storage.ApplicationData.Current.LocalFolder;
        const string AltcWidthSetting = "AltcWidth";
        const int AltcWidthDefault = 60;  // default 60 and edge case test 50, 55, 65, 70
        const string AltArrowWidthSetting = "AltArrowWidth";
        const int AltArrowWidthDefault = 40;  // default 40 and edge case test 35, 37, 47, 50
        const string CtrlArrowWidthSetting = "CtrlArrowWidth";
        const int CtrlArrowWidthDefault = 70;  // default 70 and edge case test 63, 65, 70, 75

        public ReadAndPersistSettings()
        {
            try
            {
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            }
            catch (InvalidOperationException ex)
            {
                // we'll land here directly launching full trust process .net framework winform application versus it having been launched by uwp parent application
                var message = ex.Message;
            }
        }

        public void SetAltcWidth(int width)
        {
            localSettings.Values[AltcWidthSetting] = width;
        }

        public int GetAltcWidth()
        {
            if (localSettings is null)
            {
                return AltcWidthDefault;
            }
            else 
            {
                var setting = localSettings.Values[AltcWidthSetting];
                if (setting is null) return AltcWidthDefault;
                else return Convert.ToInt32(setting);                
            }
        }

        public void SetAltArrowWidth(int width)
        {
            localSettings.Values[AltArrowWidthSetting] = width;
        }

        public int GetAltArrowWidth()
        {
            if (localSettings is null)
            {
                return AltArrowWidthDefault;
            }
            else
            {
                var setting = localSettings.Values[AltArrowWidthSetting];
                if (setting is null) return AltArrowWidthDefault;
                else return Convert.ToInt32(setting);
            }
        }

        public void SetCtrlArrowWidth(int width)
        {
            localSettings.Values[CtrlArrowWidthSetting] = width;
        }

        public int GetCtrlArrowWidth()
        {
            if (localSettings is null)
            {
                return CtrlArrowWidthDefault;
            }
            else
            {
                var setting = localSettings.Values[CtrlArrowWidthSetting];
                if (setting is null) return CtrlArrowWidthDefault;
                else return Convert.ToInt32(setting);
            }
        }
    }
}

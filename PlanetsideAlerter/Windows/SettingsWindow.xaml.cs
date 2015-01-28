using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using PlanetsideAlerter.Helpers;

namespace PlanetsideAlerter.Windows
{
    public partial class SettingsWindow : MetroWindow
    {
        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll")]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private PsAlerter _psa;

        public SettingsWindow(PsAlerter ps)
        {
            _psa = ps;
            this.DataContext = ps;
            InitializeComponent();

            var allScreens = System.Windows.Forms.Screen.AllScreens.ToList();
            for (int i = 0; i < allScreens.Count; i++)
            {
                var screen = allScreens[i];
                var device = new DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(device);
                EnumDisplayDevices(screen.DeviceName, 0, ref device, 0);

                PositionCombo.Items.Add(i + " - " + device.DeviceString + " - Bottom Right");
                PositionCombo.Items.Add(i + " - " + device.DeviceString + " - Bottom Left");
                PositionCombo.Items.Add(i + " - " + device.DeviceString + " - Top Right");
                PositionCombo.Items.Add(i + " - " + device.DeviceString + " - Top Left");
            }
            PositionCombo.SelectedIndex = Convert.ToInt32(ConfigurationManager.AppSettings.Get("placement"));

            var accent = ConfigurationManager.AppSettings.Get("accent");
            foreach (var item in AccentCombo.Items)
            {
                if ((item as AccentColorMenuData).Name == accent)
                {
                    AccentCombo.SelectedItem = item;
                    break;
                }
            }

            var servers = ConfigurationManager.AppSettings.Get("servers");
            if (servers[0] == '1')
                miller.IsChecked = true;
            if (servers[1] == '1')
                connery.IsChecked = true;
            if (servers[2] == '1')
                cobalt.IsChecked = true;
            if (servers[3] == '1')
                briggs.IsChecked = true;
            if (servers[4] == '1')
                emerald.IsChecked = true;

            startup.IsChecked = App.rkApp.GetValue(App.Name) != null;
        }

        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void ChangeTheme(object sender, SelectionChangedEventArgs e)
        {
            ((sender as ComboBox).SelectedItem as AccentColorMenuData).ChangeAccentCommand.Execute(this);
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            Configuration Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Config.AppSettings.Settings["accent"].Value = (AccentCombo.SelectedItem as AccentColorMenuData).Name;

            var servers = "";
            servers += (miller.IsChecked == true) ? "1" : "0";
            servers += (connery.IsChecked == true) ? "1" : "0";
            servers += (cobalt.IsChecked == true) ? "1" : "0";
            servers += (briggs.IsChecked == true) ? "1" : "0";
            servers += (emerald.IsChecked == true) ? "1" : "0";
            Config.AppSettings.Settings["servers"].Value = servers;

            Config.AppSettings.Settings["placement"].Value = PositionCombo.SelectedIndex.ToString();

            if (startup.IsChecked == true)
            {
                if (!App.rkApp.GetSubKeyNames().Contains(App.Name))
                    App.rkApp.SetValue(App.Name, Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                App.rkApp.DeleteValue(App.Name, false);
            }

            Config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            _psa.ScreenPopupPosition();
        }
    }
}

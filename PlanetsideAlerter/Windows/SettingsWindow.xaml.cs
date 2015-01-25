using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using PlanetsideAlerter.Helpers;

namespace PlanetsideAlerter.Windows
{
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow(PsAlerter data)
        {
            this.DataContext = data;
            InitializeComponent();

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

            if (startup.IsChecked == true)
            {
                if (!App.rkApp.GetSubKeyNames().Contains(App.Name))
                    App.rkApp.SetValue(App.Name, Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                App.rkApp.DeleteValue(App.Name, false);
            }

            Config.Save();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hardcodet.Wpf.TaskbarNotification;
using PlanetsideAlerter.Helpers;

namespace PlanetsideAlerter.Windows
{
    /// <summary>
    /// Interaction logic for AlertHolder.xaml
    /// </summary>
    public partial class AlertHolder : UserControl
    {
        private bool _isClosing = false;
        private MediaPlayer _mediaPlayer;
        public List<Alert> Alerts { get; set; }
        public AlertHolder(List<Alert> alerts)
        {
            Alerts = alerts;
            _mediaPlayer = new MediaPlayer();
            if (ApplicationDeployment.IsNetworkDeployed)
                _mediaPlayer.Open(new Uri(App.AssemblyDirectory + @"\Resources\AlertSound.mp3"));
            else
                _mediaPlayer.Open(new Uri(@"../../Resources/AlertSound.mp3", UriKind.Relative));
            _mediaPlayer.Play();
            DataContext = this;
            InitializeComponent();
        }

        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true; //suppresses the popup from being closed immediately
            _isClosing = true;
        }

        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isClosing) return;
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();
        }

        private void OnFadeOutCompleted(object sender, EventArgs e)
        {
            _mediaPlayer.Stop();
            Popup pp = (Popup)Parent;
            if(pp != null)
                pp.IsOpen = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro;

namespace PlanetsideAlerter.Helpers
{
    public class AccentColorMenuData
    {
        public string Name { get; set; }
        public Brush BorderColorBrush { get; set; }
        public Brush ColorBrush { get; set; }

        private ICommand changeAccentCommand;
        public ICommand ChangeAccentCommand
        {
            get
            {
                return this.changeAccentCommand ??
                       (changeAccentCommand =
                           new SimpleCommand
                           {
                               CanExecuteDelegate = x => true,
                               ExecuteDelegate = x => this.DoChangeTheme(x)
                           });
            }
        }

        protected virtual void DoChangeTheme(object sender)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var accent = ThemeManager.GetAccent(this.Name);
            ThemeManager.ChangeAppStyle(Application.Current, accent, ThemeManager.AppThemes.First(x => x.Name == "BaseDark"));
        }
    }

}

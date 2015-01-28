using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro;
using PlanetsideAlerter.Helpers;
using PlanetsideAlerter.Windows;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

namespace PlanetsideAlerter
{
    public class PsAlerter
    {
        public List<AccentColorMenuData> AccentColors { get; set; }
        public Dictionary<int, Event> Events { get; set; }
        public Dictionary<FactionEnum, Faction> Factions { get; set; }
        public ObservableCollection<string> Logs { get; set; }

        private Vector _popupPosition = new Vector(-1,-1);
        // -1,-1 is the default pos; bottom right of first screen
        private SettingsWindow _optionsWindow;
        private Dictionary<int, Alert> _ongoingEvents;
        private readonly TaskbarIcon _notifyIcon;
        private DispatcherTimer _timer;

        private const int PopupDuration = 10000;

        public PsAlerter(TaskbarIcon icon)
        {
            Logs = new ObservableCollection<string>();
            _ongoingEvents = new Dictionary<int, Alert>();
            _notifyIcon = icon;
            _notifyIcon.DataContext = this;
            ScreenPopupPosition();

            AccentColors = ThemeManager.Accents.Where(x => new []{"Red", "Blue", "Mauve"}.Contains(x.Name))
                                            .Select(a => new AccentColorMenuData() { Name = a.Name, ColorBrush = a.Resources["AccentColorBrush"] as Brush })
                                            .ToList();
            Logs.Add("Alert Sound Location - " + App.AssemblyDirectory + @"\Resources\AlertSound.mp3");
            CreateEventsAndFactions();
            OpenWebSocket();
        }

        public void ScreenPopupPosition()
        {
            var ppos = Convert.ToInt32(ConfigurationManager.AppSettings.Get("placement"));
            var screen = System.Windows.Forms.Screen.AllScreens[ppos/4];
            switch (ppos%4)
            {
                case 0:
                    _popupPosition = new Vector(screen.WorkingArea.Location.X + screen.WorkingArea.Width, screen.WorkingArea.Location.Y + screen.WorkingArea.Height);
                    break;
                case 1:
                    _popupPosition = new Vector(screen.WorkingArea.Location.X + 1 + 0, screen.WorkingArea.Location.Y + screen.WorkingArea.Height);
                    break;
                case 2:
                    _popupPosition = new Vector(screen.WorkingArea.Location.X + screen.WorkingArea.Width, screen.WorkingArea.Location.Y + 0);
                    break;
                case 3:
                    _popupPosition = new Vector(screen.WorkingArea.Location.X + 1 + 0, screen.WorkingArea.Location.Y + 0);
                    break;
                default:
                    _popupPosition = new Vector(-1, -1);
                    break;
            }

        }

        #region SocketStuff

        private void OpenWebSocket()
        {
            Logs.Add(DateTime.Now + " creating socket connetion");
            var ws = new WebSocket("wss://push.planetside2.com/streaming?service-id=s:psalerter");
            ws.OnOpen += OnSocketConnection;
            ws.OnMessage += ReadSocketMessage;
            ws.OnError += OnSocketError;
            ws.Connect();
        }

        private void OnSocketConnection(object sender, EventArgs e)
        {
            Logs.Add(DateTime.Now + " socket connetion succesful");
            UpdateViaXml(null, null);
            (sender as WebSocket).Send(
                "{\"service\":\"event\",\"action\":\"subscribe\",\"worlds\":[\"1\",\"9\",\"10\",\"11\",\"13\",\"17\",\"18\",\"19\",\"25\"],\"eventNames\":[\"FacilityControl\",\"MetagameEvent\"]}");
        }

        private void OnSocketError(object sender, EventArgs e)
        {
            Logs.Add(DateTime.Now + " socket connetion failed, gods help us");
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(UpdateViaXml);
            _timer.Interval = new TimeSpan(0, 5, 0);
            UpdateViaXml(null, null);
        }

        private void ReadSocketMessage(object sender, MessageEventArgs e)
        {
            if (!e.Data.Contains("\"event_name\":\"MetagameEvent\""))
                return;

            Logs.Add(DateTime.Now + " reading socket message");
            try
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    var json = JObject.Parse(e.Data);
                    var alert = new Alert(
                        json["payload"]["instance_id"].Value<int>(),
                        json["payload"]["timestamp"].Value<int>(),
                        json["payload"]["world_id"].Value<int>(),
                        Events[json["payload"]["metagame_event_id"].Value<int>()],
                        json["payload"]["metagame_event_state_name"].Value<string>() == "started",
                        Factions[GetWinner(json["payload"])]
                        );
                    if (alert.IsFinished)
                        _ongoingEvents.Remove(_ongoingEvents.First(x => x.Value.Id == alert.Id).Key);
                    else if (!_ongoingEvents.ContainsKey(alert.Id))
                        _ongoingEvents.Add(alert.Id, alert);

                    Logs.Add(DateTime.Now + " new event: " + alert.Event.Name + " at " + alert.Server);
                    
                    if (alert.Server.ToServerBit())
                    {
                        _notifyIcon.ShowCustomBalloon(
                            new AlertHolder(
                                new List<Alert>
                                {
                                    alert
                                }),
                            (int)_popupPosition.X,
                            (int)_popupPosition.Y,
                            PopupAnimation.Slide,
                            PopupDuration);
                    }
                });
            }
            catch (Exception ex)
            {
                //and fuck it
            }
        }
        
        #endregion

        private static FactionEnum GetWinner(JToken entry)
        {
            var winner = FactionEnum.VS;
            if (entry["metagame_event_state_name"].Value<string>() == "ended")
            {
                var tr = entry["faction_tr"].Value<double>();
                var nc = entry["faction_nc"].Value<double>();
                var vs = entry["faction_vs"].Value<double>();
                if (tr > nc && tr > vs)
                    winner = FactionEnum.TR;
                else if (nc > tr && nc > vs)
                    winner = FactionEnum.NC;
            }
            return winner;
        }

        #region XmlStuff
        private void ParseEvents(string e)
        {
            var json = JObject.Parse(e);
            foreach (var ev in json["metagame_event_list"])
            {
                Events.Add(
                    ev["metagame_event_id"].Value<int>(),
                    new Event()
                    {
                        Name = ev["name"]["en"].Value<string>(),
                        Description = ev["description"]["en"].Value<string>(),
                        ContinentName = ev["metagame_event_id"].Value<int>().ToContinentName()
                    });
            }
        }

        private void UpdateViaXml(object sender, EventArgs e)
        {
            var wc = new WebClient();
            Logs.Add(DateTime.Now.ToString() + " downloading metagame xml");
            wc.DownloadStringAsync(
                new Uri("http://census.soe.com/get/ps2:v2/world_event?type=METAGAME&c:limit=10"));
            wc.DownloadStringCompleted += ProcessXml;
        }

        private void ProcessXml(object sender, DownloadStringCompletedEventArgs e)
        {
            Logs.Add(DateTime.Now + " parsing metagame xml");
            var pop = false;
            var json = JObject.Parse(e.Result);
            foreach (var entry in json["world_event_list"].Where(x => x["metagame_event_state_name"].Value<string>() == "started"))
            {
                var id = entry["instance_id"].Value<int>();
                var time = entry["timestamp"].Value<int>();
                if ((int)(120 - (DateTime.Now - Alert.FromUnixTime(time).ToLocalTime()).TotalMinutes) < 0)
                    continue;

                if (!_ongoingEvents.ContainsKey(id))
                {
                    var alert = new Alert(
                        entry["instance_id"].Value<int>(),
                        entry["timestamp"].Value<int>(),
                        entry["world_id"].Value<int>(),
                        Events[entry["metagame_event_id"].Value<int>()],
                        entry["metagame_event_state_name"].Value<string>() == "started",
                        Factions[GetWinner(entry)]
                        );

                    Logs.Add(DateTime.Now + " new event: " + alert.Event.Name + " at " + alert.Server);
                    _ongoingEvents.Add(id, alert);
                    pop = true;
                }
            }

            foreach (
                var entry in
                    json["world_event_list"].Where(x => x["metagame_event_state_name"].Value<string>() == "ended"))
            {
                var id = entry["instance_id"].Value<int>();
                if (_ongoingEvents.ContainsKey(id))
                    _ongoingEvents.Remove(id);

            }

            if (pop)
                ShowAllAlerts();
        }
        #endregion

        public void ShowAllAlerts()
        {
            var rem = (from e in _ongoingEvents where e.Value.MinuteLeft < 0 select e.Key).ToList();
            foreach (var i in rem)
            {
                _ongoingEvents.Remove(i);
            }

            _notifyIcon.ShowCustomBalloon(
                new AlertHolder(
                    _ongoingEvents.Values.ToList()),
                    (int)_popupPosition.X,
                    (int)_popupPosition.Y,
                    PopupAnimation.Slide,
                    PopupDuration);
        }

        public void ShowOptionsWindow()
        {
            if(_optionsWindow == null)
                _optionsWindow = new SettingsWindow(this);
            _optionsWindow.Show();
        }

        private void CreateEventsAndFactions()
        {
            Events = new Dictionary<int, Event>();
            Factions = new Dictionary<FactionEnum, Faction>();

            Events.Add(1, new Event()
            {
                Name = "Feeling the Heat",
                Description = "Capture Indar within the time limit",
                ContinentName = "Indar"
            });

            Events.Add(2, new Event()
            {
                Name = "Cold War",
                Description = "Capture Esamir within the time limit",
                ContinentName = "Esamir"
            });

            Events.Add(3, new Event()
            {
                Name = "Seeing Green",
                Description = "Capture Amerish within the time limit",
                ContinentName = "Amerish"
            });

            Events.Add(4, new Event()
            {
                Name = "Marsh Madness",
                Description = "Capture Hossin within the time limit",
                ContinentName = "Hossin"
            });

            Factions.Add(FactionEnum.TR, new Faction()
            {
                Name = "Terran Republic",
                Flag = new BitmapImage(new Uri(String.Format("../../Resources/{0}.png", "tr"), UriKind.Relative))
            });

            Factions.Add(FactionEnum.NC, new Faction()
            {
                Name = "New Conglamerate",
                Flag = new BitmapImage(new Uri(String.Format("../../Resources/{0}.png", "nc"), UriKind.Relative))
            });

            Factions.Add(FactionEnum.VS, new Faction()
            {
                Name = "Vanu Sovereignty",
                Flag = new BitmapImage(new Uri(String.Format("../../Resources/{0}.png", "vs"), UriKind.Relative))
            });
        }

        #region Commands
        public ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => true,
                    CommandAction = () => ShowAllAlerts()
                };
            }
        }

        public ICommand ShowOptionsCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => true,
                    CommandAction = () => ShowOptionsWindow()
                };
            }
        }

        public ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        } 
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PlanetsideAlerter.Helpers
{
    public class Alert
    {
        public int Id { get; set; }
        public string Description
        {
            get
            {
                if (IsFinished)
                    return Winner.Name.ToUpperInvariant();
                else
                    return Event.Description + " (" + MinuteLeft + "m)";

            }
            set { }
        }
        public string Server { get; set; }
        public string State { get; set; }
        public DateTime Time { get; set; }
        public bool IsFinished { get; set; }
        public Faction Winner { get; set; }
        public Event Event { get; set; }

        public int MinuteLeft
        {
            get
            {
                return (int)(120 - (DateTime.Now - Time).TotalMinutes);
            }
        }

        public Alert(int id, int time, int server, Event e, bool isStarted, Faction winner)
        {
            Id = id;
            var t = FromUnixTime(time).ToLocalTime();
            Time = t;
            Event = e;
            Server = server.ToServerName().ToUpperInvariant();
            State = isStarted ? "STARTED" : "ENDED";
            Winner = winner;
            IsFinished = !isStarted;
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }

    public class Event
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ContinentName { get; set; }
    }

    public enum FactionEnum
    {
        VS,
        NC,
        TR
    }

    public class Faction
    {
        public string Name { get; set; }
        public ImageSource Flag { get; set; }
    }
}

using System;

namespace TournamentAPI
{
    public class Tournament
    {
        public int id;
        public string name;
        public int templateId;
        public string description;
        public DateTime startDate;
        public DateTime endDate;
        public int organiser;
        public bool hasStarted;
    }
}
using System.Collections.Generic;

namespace TournamentAPI
{
    public class Group
    {
        public int id;
        public string name;
        public List<Player> playersInGroup = new List<Player>();
        public int roundId;
    }
}

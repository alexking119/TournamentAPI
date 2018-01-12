using System;
using System.Collections.Generic;
using System.Text;

namespace TournamentAPI.Tests
{
    public class TournamentGeneratorMock : TournamentGenerator
    {
        public List<Group> CreateGroups(IDataStore datastore, Player[] tournamentPlayers, int roundId)
        {
            return base.CreateGroups(datastore, tournamentPlayers, roundId);
        }

        public List<Game> CreateGamesFromGroup(Group group)
        {
            return base.CreateGamesFromGroup(group);
        }
    }
}

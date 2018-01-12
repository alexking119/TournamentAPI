using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TournamentAPI
{
    public enum RoundTypes
    {
        ANY_TYPE = -1,
        GROUP = 1,
        KNOCKOUT_64 = 2,
        KNOCKOUT_32 = 3,
        KNOCKOUT_16 = 4,
        KNOCKOUT_8 = 5,
        QUATERFINALS = 6,
        SEMIFINALS = 7,
        FINALS = 8,
        KNOCKOUT_ANY = 9,
    }
}

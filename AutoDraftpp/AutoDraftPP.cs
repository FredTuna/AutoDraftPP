using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;
using System.Text.RegularExpressions;

namespace AutoDraftPP
{
    using YearToStatLineDict = System.Collections.Generic.Dictionary<int, PlayerSeasonStatLine>;

    static class Constants
    {
        static public readonly int CurrentYear = 2017;
        static public readonly double[] PreviousGPGWeights = { 0.6, 0.25, 0.15};
    }

    [DelimitedRecord(",")]
    public class PlayerSeasonStatLine
    {
        public string Name;
        public string FirstName;
        public string Position;
        public string Team;
        public int GamesPlayed;
        public int Goals;
        public int Assists;
        public int Points;
        public double PointsPerGame;
        public int PlusMinus;
        public int PenaltyMinutes;
        public int Hits;
        public int BlockedShots;
        public int PowerplayGoals;
        public int PowerplayAssists;
        public int ShorthandedGoals;
        public int ShorthandedAssists;
        public int GameWinningGoals;
        public int ShotsOnGoal;
        public double ShotPercentage;
    }  

    [DelimitedRecord(",")]
    public class PlayerSeasonExpectedStatLine
    {
        public string Name;
        public string FirstName;
        public double PointsPerGame;
    }

    public class PlayerName
    {
        public PlayerName(string name, string firstName)
        {
            Name = name;
            FirstName = firstName;
        }

        public string Name;
        public string FirstName;
    }
    class NameEqualityComparer : IEqualityComparer<PlayerName>
    {
        public bool Equals(PlayerName p1, PlayerName p2)
        {
            if (p2 == null && p1 == null)
                return true;
            else if (p2 == null | p1 == null)
                return false;
            else if (p2.Name == p1.Name && p2.FirstName == p1.FirstName)
                return true;
            else
                return false;
        }

        public int GetHashCode(PlayerName playerName)
        {
            string hCode = playerName.Name + playerName.FirstName;
            return hCode.GetHashCode();
        }
    }

    class AutoDraftPP
    {
        static void Main(string[] args)
        {
            //////////////////////////////////////////////
            // Read existing player stats and store them

            //Hold every year's stats for every player using their fullname as key
            Dictionary<PlayerName, YearToStatLineDict> playersToStatLines 
                = new Dictionary<PlayerName, YearToStatLineDict>(new NameEqualityComparer());

            var engineReader = new FileHelperEngine<PlayerSeasonStatLine>();
            
            //Read every file passed in arguments
            foreach (string seasonStatsFile in args)
            {
                var seasonStatLines = engineReader.ReadFile(seasonStatsFile);

                //Extract year from file name
                int year = Int32.Parse(Regex.Replace(seasonStatsFile, @"[^\d]+", ""));

                //Read every player's statline from that year and add them to the player dictionary
                foreach (PlayerSeasonStatLine statLine in seasonStatLines)
                {
                    PlayerName playerName = new PlayerName(statLine.Name, statLine.FirstName);

                    YearToStatLineDict yearsToStatLines;
                    if (playersToStatLines.TryGetValue(playerName, out yearsToStatLines))
                    {
                        yearsToStatLines.Add(year, statLine);
                    }
                    else
                    {
                        yearsToStatLines = new YearToStatLineDict();
                        yearsToStatLines.Add(year, statLine);
                        playersToStatLines.Add(playerName, yearsToStatLines);
                    }
                }                
            }

            //////////////////////////////////////////////
            // Calculate Expected stats and write them out
            List<PlayerSeasonExpectedStatLine> playerToExpectedStatLine 
                = new List<PlayerSeasonExpectedStatLine>();

            foreach ( var playerToStatLines in playersToStatLines)
            {

                PlayerName playerName = playerToStatLines.Key;
                double totalUsedWeight = 1;
                double expectedPPG = 0; 

                //check every year with given weight and calculate expected ppg
                for (int i = 0; i < Constants.PreviousGPGWeights.Count(); i++)
                {
                    double weight = Constants.PreviousGPGWeights[i];
                    PlayerSeasonStatLine statLine;
                    if (playerToStatLines.Value.TryGetValue(Constants.CurrentYear - (i + 1), out statLine))
                    {
                        expectedPPG += statLine.PointsPerGame * weight;
                    }
                    else //missed a year, account for it later
                        totalUsedWeight -= weight;
                }

                //If the player missed some years, adjust the value 
                if (totalUsedWeight > 0)
                    expectedPPG *= 1 / totalUsedWeight;

                PlayerSeasonExpectedStatLine expectedStatLine = new PlayerSeasonExpectedStatLine();
                expectedStatLine.PointsPerGame = expectedPPG;
                expectedStatLine.FirstName = playerName.FirstName;
                expectedStatLine.Name = playerName.Name;

                playerToExpectedStatLine.Add(expectedStatLine);
            }

            var engineWriter = new FileHelperEngine<PlayerSeasonExpectedStatLine>();
            engineWriter.WriteFile("ExpectedStats.csv", playerToExpectedStatLine);

        }
    }
}

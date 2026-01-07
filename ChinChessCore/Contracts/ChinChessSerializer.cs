using IceTea.Pure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChinChessCore.Contracts
{
    public static class ChinChessSerializer
    {
        public static string Serialize(IList<ChinChessInfo> chesses)
        {
            chesses.AssertNotEmpty(nameof(chesses));

            return string.Join(",", chesses.Select(c => c.ToString()));
        }

        public static IList<ChinChessInfo> Deserialize(string chesses)
        {
            if (string.IsNullOrEmpty(chesses) && !Regex.IsMatch(chesses, "^([A-Ga-g][0-9][0-8],)*([A-Ga-g][0-9][0-8])+$"))
            {
                throw new InvalidOperationException();
            }

            return chesses.Split(',').Select(str => ChinChessInfo.Load(str)).ToList();
        }
    }
}

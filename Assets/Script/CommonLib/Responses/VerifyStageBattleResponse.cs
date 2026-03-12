using System;
using System.Collections.Generic;

namespace Script.CommonLib.Responses
{
    public class VerifyStageBattleResponse
    {
        public TeamFlag Winner { get; set; }
        public List<Tuple<uint, uint>> AliveEntities { get; set; }
            
        // Deserialize용 생성자
        public VerifyStageBattleResponse() { }
    }
}
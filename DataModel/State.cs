using System.Collections.ObjectModel;

namespace DBF.DataModel
{
    public struct State
    {
        public List<BridgeTimerState> TimerStates    { get; set; }
        public string                 MainClubName   { get; set; }
        public string                 SubClubName    { get; set; }
        public string                 PlayingTimeStr { get; set; }

        public List<BridgeTimer>      BridgeTimers   { get; set; }
    }
}

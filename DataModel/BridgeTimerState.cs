using System.Xml.Serialization;

namespace DBF.DataModel
{
    public struct BridgeTimerState
    {
        public bool     IsStarted      { get; set; }
        public bool     IsAtBreak      { get; set; }
        public bool     IsAtTransition { get; set; }
        public bool     IsPaused       { get; set; }
        public TimeSpan RemainingTime  { get; set; }
        public int      Round          { get; set; }
    }
}

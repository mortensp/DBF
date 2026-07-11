using System.ComponentModel;
using Caliburn.Micro;

namespace DBF
{
    // Declare INotifyPropertyChanged event so it's visible at compile time.
    // PropertyChanged.Fody will still weave the calls to raise this event when auto-properties change.
    public class RoundStatus : PropertyChangedBase
    {
        public          short Section       { get; set; }
        public          string Letter        { get; set; }
        public          short Round         { get; set; }
        public          bool   Done          { get; set; }
        public          int    BoardsRemaining { get; set; }

        public override string ToString() => $"Section:{Section}-{Letter} ,Round:{Round}, Done:{Done}, Remaing:{BoardsRemaining}";
    }
}

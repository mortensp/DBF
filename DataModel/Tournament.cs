using System.Xml.Serialization;
using String.Localization;

namespace DBF.DataModel
{
    /// A Tournament corresponds to a section - e.g., the B-Section
    [XmlRoot(ElementName = "Tournament")]
    public class Tournament
    {
        public bool              CalculateHAC           => CalculateHACStr.AsBool();
        public                                                      int               SectionNo              { get; set; } = 1;

        //-----
        [XmlElement(ElementName = "ClubId")] public                 string            ClubId                 { get; set; }
        [XmlElement(ElementName = "Description")] public            string            Description            { get; set; }
        [XmlElement(ElementName = "TournamentType")] public         TournamentType    TournamentType         { get; set; }
        [XmlElement(ElementName = "MovementPlan")] public           string            MovementPlan           { get; set; }
        [XmlElement(ElementName = "MovementPlanType")] public       string            MovementPlanType       { get; set; }
        [XmlElement(ElementName = "SubMovementPlanType")] public    string            SubMovementPlanType    { get; set; }
        [XmlElement(ElementName = "TournamentPairCalcType")] public string            TournamentPairCalcType { get; set; }
        [XmlElement(ElementName = "TournamentTeamType")] public     string            TournamentTeamType     { get; set; }
        [XmlElement(ElementName = "CalculateHAC")] public           string            CalculateHACStr        { get; set; }
        [XmlElement(ElementName = "SimplifiedHAC")] public          string            SimplifiedHAC          { get; set; }
        [XmlElement(ElementName = "GiveHACPrizes")] public          string            GiveHACPrizes          { get; set; }
        [XmlElement(ElementName = "IsSwiss")] public                string            IsSwiss                { get; set; }
        [XmlElement(ElementName = "IsBAM")] public                  string            IsBAM                  { get; set; }
        [XmlElement(ElementName = "IsKnockout")] public             string            IsKnockout             { get; set; }
        [XmlAttribute(AttributeName = "GroupNo")] public            string            GroupNo                { get; set; }
        [XmlAttribute(AttributeName = "GroupName")] public          string            groupName              { get; set; }
        [XmlElement(ElementName = "Section")] public                List<SectionFile> SectionFiles           { get; set; }

        //-----
        public SectionFile       SectionFile            => SectionFiles[SectionNo - 1];

        public string Group
        {
            get
            {
                if (string.IsNullOrEmpty(groupName))
                    return string.Empty;

                // If second char is separator, first char is the group letter
                if (groupName.Length >= 2)
                {
                    var ch2 = groupName[1];

                    if (ch2 is ' ' or '-' or ':')
                        return groupName[0].ToString();
                }

                // Fast path: check current-culture translations (cheap)
                var name   = groupName;
                var curRed = Lex.Red;

                if (!string.IsNullOrEmpty(curRed)
                &&  name.Contains(" " + curRed, StringComparison.OrdinalIgnoreCase))
                    return "A";

                var curYellow = Lex.Yellow;

                if (!string.IsNullOrEmpty(curYellow)
                &&  name.Contains(" " + curYellow, StringComparison.OrdinalIgnoreCase))
                    return "B";

                var curBlue = Lex.Blue;

                if (!string.IsNullOrEmpty(curBlue)
                &&  name.Contains(" " + curBlue, StringComparison.OrdinalIgnoreCase))
                    return "C";

                // Fallback: check all localized translations for each key
                foreach (var kv in LanguageService.GetTranslations(() => Lex.Red))
                    if (!string.IsNullOrEmpty(kv.Value) && name.Contains(" " + kv.Value, StringComparison.OrdinalIgnoreCase))
                        return "A";

                foreach (var kv in LanguageService.GetTranslations(() => Lex.Yellow))
                    if (!string.IsNullOrEmpty(kv.Value) && name.Contains(" " + kv.Value, StringComparison.OrdinalIgnoreCase))
                        return "B";

                foreach (var kv in LanguageService.GetTranslations(() => Lex.Blue))
                    if (!string.IsNullOrEmpty(kv.Value) && name.Contains(" " + kv.Value, StringComparison.OrdinalIgnoreCase))
                        return "C";

                // Last-resort: literal Danish names (keeps compatibility)
                if (name.Contains("Rød", StringComparison.OrdinalIgnoreCase))
                    return "A";

                if (name.Contains("Gul", StringComparison.OrdinalIgnoreCase))
                    return "B";

                if (name.Contains("Blå", StringComparison.OrdinalIgnoreCase))
                    return "C";

                return " ";
            }
        }

        public string GroupName
        {
            get
            {
                if (groupName        is not null
                &&  groupName.Length >  2)
                {
                    char ch1 = groupName[0];
                    char ch2 = groupName[1];

                    var text = (ch2 is ' ' or '-')
                             ? groupName.Substring(2)
                             : groupName;

                    if (groupName.Contains("Rød"))
                        return "Rød række";

                    if (groupName.Contains("Gul"))
                        return "Gul (grå) række";

                    if (groupName.Contains("Blå"))
                        return "Blå række";
                }

                return "-Rækken";
            }
        }

        public string Title
        {
            get
            {
                return $"{Group} - {GroupName}:  {TournamentType.Text} ({MovementPlan})";
            }
        }
    }
}

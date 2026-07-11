//using NAudio.Wave;
using System.Linq;

namespace DBF.AudioServices;

public static class AudioResources
{
    public static readonly SoundDefinition[] SoundDefinitions;

    public static readonly SoundDefinition Sound_Bowl           = new( Lex.Sound_Bowl);
    public static readonly SoundDefinition Sound_Boxing         = new (Lex.Sound_Boxing);
    public static readonly SoundDefinition Sound_Boxing2        = new(Lex.Sound_Boxing2) ;
    public static readonly SoundDefinition Sound_DingDing       = new(Lex.Sound_DingDing) ;
    public static readonly SoundDefinition Sound_DinnerReady    = new(Lex.Sound_DinnerReady) ;
    public static readonly SoundDefinition Sound_HandHeld       = new(Lex.Sound_HandHeld) ;
    public static readonly SoundDefinition Sound_IonBell        = new(Lex.Sound_IonBell) ;
    public static readonly SoundDefinition Sound_MechanicalBell = new(Lex.Sound_MechanicalBell) ;
    public static readonly SoundDefinition Sound_Notify         = new(Lex.Sound_Notify) ;
    public static readonly SoundDefinition Sound_School         = new(Lex.Sound_School) ;
    public static readonly SoundDefinition Sound_School2        = new(Lex.Sound_School2) ;

    static AudioResources()
    {
        SoundDefinitions =
        new[]
        {
            Sound_Bowl
          , Sound_Boxing
          , Sound_Boxing2
          , Sound_DingDing
          , Sound_DinnerReady
          , Sound_HandHeld
          , Sound_IonBell
          , Sound_MechanicalBell
          , Sound_School
          , Sound_School2
        };
    }
}

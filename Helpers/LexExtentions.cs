using String.Localization;
namespace DBF.Helpers;

public static class LexExtentions
{
    public static string GetKeyForTranslation(this string displayName)
                    => LanguageService.GetKeyForTranslation(typeof(Lex), displayName);


    public static string GetTranslation(this string key)
                        => LanguageService.GetTranslation(typeof(Lex), key);
}

namespace VLC.Net.Core.Helpers;
internal static class LanguageHelper
{
    private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse("Screenbox.Core/LanguageCodes");

    public static bool TryConvertIso6392ToIso6391(string threeLetterTag, out string twoLetterTag)
    {
        twoLetterTag = string.Empty;
        if (threeLetterTag.Length != 3) return false;
        twoLetterTag = Loader.GetString(threeLetterTag);
        return !string.IsNullOrEmpty(twoLetterTag);
    }

    public static string GetPreferredLanguage()
    {
        return Windows.System.UserProfile.GlobalizationPreferences.Languages.FirstOrDefault() ?? string.Empty;
    }
}

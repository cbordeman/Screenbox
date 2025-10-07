#nullable enable
namespace VLC.Net.Core.Models;
public sealed record Language(string NativeName, string LanguageTag, LanguageLayoutDirection LayoutDirection)
{
    public string NativeName { get; set; } = NativeName;

    public string LanguageTag { get; set; } = LanguageTag;

    public LanguageLayoutDirection LayoutDirection { get; set; } = LayoutDirection;
}

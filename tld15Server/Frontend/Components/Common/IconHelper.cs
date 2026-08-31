using System.Linq;

namespace tld15Server.Frontend.Components.Common;

public static class IconHelper
{
    public static string GetLanguage(string key)
    {
        var language = key.Split("_").LastOrDefault();

        if (string.IsNullOrEmpty(language) || language.Length > 3)
        {
            return "〇〇";
        }

        return language.ToUpper();
    }

    public static string GetIcon(string name)
    {
        var key = name.Split("_")[0].ToLowerInvariant();

        return key switch
        {
            "amazon" => "/images/icons/amazon.svg",
            "email" => "/images/icons/email.svg",
            "facebook" => "/images/icons/facebook.svg",
            "github" => "/images/icons/github.svg",
            "instagram" => "/images/icons/instagram.svg",
            "itch" => "/images/icons/itch.svg",
            "linkedin" => "/images/icons/linkedin.svg",
            "pirate" => "/images/icons/pirate.svg",
            "pixiv" => "/images/icons/pixiv.svg",
            "royalroad" => "/images/icons/royalroad.svg",
            "steam" => "/images/icons/steam.svg",
            "telegram" => "/images/icons/telegram.svg",
            "youtube" => "/images/icons/youtube.svg",
            "rss" => "/images/icons/rss.svg",

            _ => "/images/icons/default.svg",
        };
    }
}

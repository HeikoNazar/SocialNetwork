using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using SocialNetwork.Data.Helpers;

namespace SocialNetwork.Extensions
{
    public static class HashtagLinkExtensions
    {
        // Writes the text safely (HTML encoded) and turns every #hashtag into a link to its page
        public static IHtmlContent WithHashtagLinks(this IHtmlHelper html, string? text)
        {
            var builder = new HtmlContentBuilder();
            if (string.IsNullOrEmpty(text))
            {
                return builder;
            }

            var urlHelper = html.ViewContext.HttpContext.RequestServices
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(html.ViewContext);
            int last = 0;

            foreach (Match match in HashtagParser.Pattern.Matches(text))
            {
                builder.Append(text[last..match.Index]);

                var link = new TagBuilder("a");
                link.Attributes["href"] = urlHelper.Action("Hashtag", "Home", new { name = match.Groups[1].Value.ToLowerInvariant() });
                link.InnerHtml.Append(match.Value);
                builder.AppendHtml(link);

                last = match.Index + match.Length;
            }

            builder.Append(text[last..]);
            return builder;
        }
    }
}
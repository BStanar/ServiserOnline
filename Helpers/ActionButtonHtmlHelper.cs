using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ServiserOnline.Helpers
{
    public static class ActionButtonHtmlHelper
    {
        public static IHtmlContent ActionButton(
            this IHtmlHelper htmlHelper,
            string text,
            string icon,
            string style,
            string extraClasses = "",
            string type = "button",
            string? id = null,
            object? htmlAttributes = null)
        {
            var button = new TagBuilder("button");

            button.Attributes["type"] = type;

            if (!string.IsNullOrWhiteSpace(id))
            {
                button.Attributes["id"] = id;
            }

            button.AddCssClass($"btn-action btn-action-{style}");

            if (!string.IsNullOrWhiteSpace(extraClasses))
            {
                button.AddCssClass(extraClasses);
            }

            if (htmlAttributes != null)
            {
                var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

                foreach (var attr in attributes)
                {
                    button.Attributes[attr.Key.Replace("_", "-")] = attr.Value?.ToString();
                }
            }

            var iconTag = new TagBuilder("i");
            iconTag.AddCssClass($"bi {icon}");

            var spanTag = new TagBuilder("span");
            spanTag.InnerHtml.Append(text);

            button.InnerHtml.AppendHtml(iconTag);
            button.InnerHtml.AppendHtml(spanTag);

            return button;
        }
    }
}
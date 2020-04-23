using AnyConsole;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Html;
using System.Text;
using System.Text.RegularExpressions;
using System;

namespace NUnit.Commander.Display
{
    public static class HtmlPrettify
    {
        /// <summary>
        /// Format a json string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="colorScheme"></param>
        /// <returns></returns>
        public static ColorTextBuilder Format(string text, ColorScheme colorScheme)
        {
            try
            {
                var builder = new ColorTextBuilder();

                var startIndex = text.IndexOf('<');
                if (startIndex > 0)
                {
                    builder.AppendLine(text.Substring(0, startIndex), colorScheme.DarkDefault);
                    text = text.Substring(startIndex, text.Length - startIndex);
                }
                var parser = new HtmlParser();
                var document = parser.ParseDocument(text);
                var writer = new StringWriter();
                document.ToHtml(writer, new PrettyMarkupFormatter
                {
                    Indentation = "  ",
                    NewLine = Environment.NewLine
                });
                var html = writer.ToString();

                // html is prettified, now we need to color code it
                var buffer = new StringBuilder();
                foreach (var c in html)
                {
                    if (c == '<' && buffer.Length > 0)
                    {
                        // add contents of tag
                        builder.Append(buffer.ToString(), colorScheme.Default);
                        buffer.Clear();
                    }
                    buffer.Append(c);
                    var match = Regex.Match(buffer.ToString(), "<.+?>");
                    if (match.Success)
                    {
                        // matched a tag, append it
                        builder.Append(buffer.ToString(), colorScheme.DarkDefault);
                        buffer.Clear();
                    }
                }

                return builder;
            }
            catch (Exception)
            {
                // something went wrong
                return ColorTextBuilder.Create.Append(text);
            }
        }
    }
}

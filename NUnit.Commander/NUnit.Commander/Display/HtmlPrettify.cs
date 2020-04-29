using AngleSharp.Html;
using AngleSharp.Html.Parser;
using AnyConsole;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

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
                // fix badly encoded strings
                if (text.Contains(@"\r") || text.Contains(@"\n"))
                {
                    text = text.Replace("\r", "").Replace("\n", "");
                    text = text.Replace(@"\r", "\r");
                    text = text.Replace(@"\n", "\n");
                }
                text = text.Replace(@"\\\\", @"\\");

                var startIndex = text.IndexOf('<');
                if (startIndex > 0)
                {
                    builder.AppendLine(text.Substring(0, startIndex), colorScheme.DarkDefault);
                    text = text.Substring(startIndex, text.Length - startIndex);
                }
                var parser = new HtmlParser();
                parser.Error += Parser_Error;
                parser.Parsing += Parser_Parsing;
                var document = parser.ParseDocument(text);
                var writer = new StringWriter();
                document.ToHtml(writer, new PrettyMarkupFormatter
                {
                    Indentation = "  ",
                    NewLine = Environment.NewLine
                });
                var html = writer.ToString();

                if (html.Length > 0)
                {
                    // html is prettified, now we need to color code it
                    var buffer = new StringBuilder();
                    var contentsColor = colorScheme.Default;
                    var isInComment = false;
                    foreach (var c in html)
                    {
                        if (c == '>' && isInComment)
                        {
                            // comment detection
                            buffer.Append(c);
                            var val = buffer.ToString();
                            if (val.EndsWith("-->"))
                            {
                                isInComment = false;
                                // if this is a stack trace, find the start
                                var stStartIndex = val.IndexOf(" at ");
                                if (stStartIndex >= 0 && val.Contains(" in "))
                                {
                                    // found a stack trace in a comment
                                    if (stStartIndex > 0)
                                        builder.Append(val.Substring(0, stStartIndex), contentsColor);
                                    var stEndIndex = val.IndexOf("-->");
                                    var stackTrace = val.Substring(stStartIndex, stEndIndex - stStartIndex);

                                    builder.AppendLine(StackTracePrettify.Format(stackTrace, colorScheme));
                                    if (stEndIndex < val.Length)
                                        builder.Append(val.Substring(stEndIndex, val.Length - stEndIndex), contentsColor);
                                }
                                else
                                    builder.Append(val, contentsColor);
                                buffer.Clear();
                                contentsColor = colorScheme.Default;
                                continue;
                            }
                        }
                        if (isInComment)
                        {
                            buffer.Append(c);
                            continue;
                        }
                        if (c == '<' && buffer.Length > 0 && !isInComment)
                        {
                            // add contents of tag
                            var val = buffer.ToString();
                            // detect stack traces
                            if (val.Contains("<!--"))
                            {
                                // stack traces in ASP.Net are usually embedded in an html comment
                                contentsColor = colorScheme.DarkSuccess;
                                isInComment = true;
                                buffer.Append(c);
                                continue;
                            }
                            if (val.Contains(" at ") && val.Contains(" in "))
                            {
                                if (!Console.IsOutputRedirected)
                                    builder.Append(StackTracePrettify.Format(HttpUtility.HtmlDecode(val), colorScheme));
                                else
                                    builder.Append(HttpUtility.HtmlDecode(val), contentsColor);
                            }
                            else
                                builder.Append(HttpUtility.HtmlDecode(val), contentsColor);
                            buffer.Clear();
                        }
                        buffer.Append(c);
                        var match = Regex.Match(buffer.ToString(), "<.+?>");
                        if (match.Success)
                        {
                            // matched a tag, append it
                            contentsColor = colorScheme.Default;
                            var color = colorScheme.DarkDefault;
                            var tag = buffer.ToString();
                            switch (tag.ToLower().Replace("<", "").Replace(">", "").Replace("/", ""))
                            {
                                case "code":
                                case "pre":
                                    color = colorScheme.DarkDuration;
                                    contentsColor = colorScheme.Default;
                                    break;
                            }
                            if (tag.Contains("<!--"))
                                color = colorScheme.DarkSuccess;
                            builder.Append(tag, color);
                            buffer.Clear();
                        }
                    }
                }
                else
                {
                    // html not found, output the full original text
                    builder.AppendLine(text, colorScheme.Default);
                }

                return builder;
            }
            catch (Exception)
            {
                // something went wrong
                return ColorTextBuilder.Create.Append(text);
            }
        }

        private static void Parser_Parsing(object sender, AngleSharp.Dom.Events.Event ev)
        {
        }

        private static void Parser_Error(object sender, AngleSharp.Dom.Events.Event ev)
        {
        }
    }
}

using AnyConsole;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;

namespace NUnit.Commander.Display
{
    public static class JsonPrettify
    {
        private const string TabValue = "  ";

        /// <summary>
        /// Format a json string
        /// </summary>
        /// <param name="text"></param>
        /// <param name="colorScheme"></param>
        /// <returns></returns>
        public static ColorTextBuilder Format(string text, ColorScheme colorScheme)
        {
            var builder = new ColorTextBuilder();

            try
            {
                // find the json in a response
                var startIndex = text.IndexOf("{");
                var endIndex = text.LastIndexOf("}");
                // make sure braces are all present to validate the json
                var braceCount = text.Count(c => c == '{') + text.Count(c => c == '}');
                var json = string.Empty;
                if (startIndex >= 0 && endIndex > 0 && braceCount % 2 == 0)
                {
                    json = text.Substring(startIndex, (endIndex + 1) - startIndex);
                    // fix badly encoded strings
                    json = json.Replace(@"\\r\\n", @"\r\n");
                    json = json.Replace(@"\\\\", @"\\");
                }

                // append any text before the json content
                if (startIndex > 0)
                    builder.AppendLine(text.Substring(0, startIndex), colorScheme.Default);

                if (!string.IsNullOrEmpty(json))
                {
                    var parsed = JObject.Parse(json, new JsonLoadSettings { 
                        CommentHandling = CommentHandling.Load, 
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Ignore, 
                        LineInfoHandling = LineInfoHandling.Load 
                    });
                    var childrenCount = parsed.Count;
                    var item = 0;
                    builder.AppendLine("{", colorScheme.DarkDefault);
                    foreach (JProperty node in parsed.Children())
                    {
                        item++;
                        WriteNode(builder, node.Name, node.Value, 1, colorScheme);
                        if (item < childrenCount)
                            builder.AppendLine($",");
                        else
                            builder.AppendLine();
                    }
                    builder.AppendLine("}", colorScheme.DarkDefault);
                }
                else
                {
                    // json not found, output the full original text
                    builder.AppendLine(text, colorScheme.Default);
                }

                // append any text after the json content
                if (endIndex < text.Length)
                    builder.AppendLine(text.Substring(endIndex + 1, text.Length - (endIndex + 1)), colorScheme.Default);
                return builder;
            }
            catch (Exception)
            {
                // something went wrong
                return ColorTextBuilder.Create.Append(text);
            }
        }

        private static void WriteNode(ColorTextBuilder builder, string key, JToken value, int tabCount, ColorScheme colorScheme)
        {
            if (!string.IsNullOrEmpty(key))
            {
                builder.Append(Repeat(TabValue, tabCount));
                builder.Append(Quote(key) + ": ", colorScheme.DarkDefault);
            }
            var childrenCount = 0;
            var item = 0;
            switch (value.Type)
            {
                case JTokenType.Array:
                    childrenCount = value.Count();
                    var isLargeArray = childrenCount > 2;
                    if (isLargeArray)
                        builder.Append("[", colorScheme.DarkDefault);
                    else
                        builder.AppendLine("[", colorScheme.DarkDefault);
                    var isTightArray = false;
                    foreach (var node in value)
                    {
                        item++;
                        isTightArray = isLargeArray && node.Type != JTokenType.Object && node.Type != JTokenType.Array;
                        if (isTightArray)
                        {
                            // print arrays without line breaks
                            WriteNode(builder, string.Empty, node, (tabCount + 1), colorScheme);
                            if (item < childrenCount)
                                builder.Append($", ", colorScheme.DarkDefault);
                        }
                        else
                        {
                            if (item == 1)
                                builder.AppendLine();
                            builder.Append(Repeat(TabValue, (tabCount + 1)));
                            WriteNode(builder, string.Empty, node, (tabCount + 1), colorScheme);
                            if (item < childrenCount)
                                builder.AppendLine($", ", colorScheme.DarkDefault);
                            else
                                builder.AppendLine();
                        }
                    }
                    if (!isTightArray)
                        builder.Append(Repeat(TabValue, tabCount));
                    builder.Append("]", colorScheme.DarkDefault);
                    break;
                case JTokenType.Object:
                    builder.AppendLine("{", colorScheme.DarkDefault);
                    childrenCount = value.Count();
                    foreach (JProperty node in value)
                    {
                        item++;
                        WriteNode(builder, node.Name, node.Value, (tabCount + 1), colorScheme);
                        if (item < childrenCount)
                            builder.AppendLine($",", colorScheme.DarkDefault);
                        else
                            builder.AppendLine();
                    }
                    builder.Append(Repeat(TabValue, tabCount));
                    builder.Append("}", colorScheme.DarkDefault);
                    break;
                default:
                    WriteValue(builder, value.Value<string>(), value.Type, colorScheme);
                    break;
            }
        }

        private static void WriteValue(ColorTextBuilder builder, string value, JTokenType type, ColorScheme colorScheme)
        {
            switch (type)
            {
                case JTokenType.String:
                case JTokenType.Uri:
                case JTokenType.Guid:
                    var val = value;
                    // detect stack traces
                    if (val.Contains(" at ") && val.Contains(" in ") && val.Contains(":line"))
                    {
                        if (!Console.IsOutputRedirected)
                        {
                            builder.Append(@"""");
                            // don't encode the contents with valid json, we are more concerned about readability here
                            builder.Append(StackTracePrettify.Format(val, colorScheme));
                            builder.Append(@"""");
                        }
                        else
                            builder.Append(Quote(Encode(val)), colorScheme.Default);
                    }
                    else
                        builder.Append(Quote(Encode(val)), colorScheme.Default);
                    break;
                case JTokenType.Null:
                    builder.Append("null", colorScheme.DarkDuration);
                    break;
                case JTokenType.Comment:
                    // this doesn't work with Newtonsoft's current JObject parser.
                    builder.Append(value, colorScheme.DarkSuccess);
                    break;
                case JTokenType.Boolean:
                    builder.Append(value.ToLower(), colorScheme.Duration);
                    break;
                case JTokenType.Undefined:
                case JTokenType.Bytes:
                case JTokenType.Date:
                case JTokenType.Float:
                case JTokenType.Integer:
                case JTokenType.TimeSpan:
                    builder.Append(value, colorScheme.Duration);
                    break;
            }
        }

        private static string Repeat(string str, int count)
        {
            var sb = new StringBuilder(str.Length * count);
            for (var i = 0; i < count; i++)
                sb.Append(str);
            return sb.ToString();
        }

        private static string Encode(string str)
        {
            return str.Replace(@"\", @"\\")
                .Replace(@"""", @"\""")
                .Replace("\b", @"\\b")
                .Replace("\f", @"\\f")
                .Replace("\n", @"\\n")
                .Replace("\r", @"\\r")
                .Replace("\t", @"\\t");
        }

        private static string Quote(string str)
        {
            return $"\"{str}\"";
        }
    }
}

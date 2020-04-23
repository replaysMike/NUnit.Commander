using AnyConsole;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace NUnit.Commander.Display
{
    public static class JsonPrettify
    {
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
                    json = text.Substring(startIndex, (endIndex + 1) - startIndex);

                if (startIndex > 0)
                    builder.AppendLine(text.Substring(0, startIndex), colorScheme.DarkDefault);

                if (!string.IsNullOrEmpty(json))
                {
                    using (var stringReader = new StringReader(json))
                    {
                        using (var stringWriter = new StringWriter())
                        {
                            var jsonReader = new JsonTextReader(stringReader);
                            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                            jsonWriter.WriteToken(jsonReader);
                            builder.AppendLine(stringWriter.ToString(), colorScheme.Default);
                        }
                    }
                }

                if (endIndex < text.Length)
                    builder.AppendLine(text.Substring(endIndex, text.Length - (endIndex + 1)), colorScheme.DarkDefault);
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

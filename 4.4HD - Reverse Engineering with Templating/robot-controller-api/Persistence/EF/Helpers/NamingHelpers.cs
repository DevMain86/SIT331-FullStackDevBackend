using System;
using System.Globalization;
using System.Text;
using HandlebarsDotNet;

namespace robot_controller_api.Persistence.EF.Helpers
{
    public static class NamingHelpers
    {
        // Register Handlebars helpers used by templates
        public static void RegisterHelpers()
        {
            Handlebars.RegisterHelper("toPascal", (writer, context, parameters) =>
            {
                // Guard: handle missing parameter
                if (parameters.Length == 0 || parameters[0] == null) { writer.Write(""); return; }
                var s = parameters[0]?.ToString() ?? string.Empty;
                writer.Write(ToPascalCase(s));
            });

            Handlebars.RegisterHelper("toSnake", (writer, context, parameters) =>
            {
                // Guard: handle missing parameter
                if (parameters.Length == 0 || parameters[0] == null) { writer.Write(""); return; }
                var s = parameters[0]?.ToString() ?? string.Empty;
                writer.Write(ToSnakeCase(s));
            });
        }

        // Convert a string to PascalCase (safe for null/empty input)
        public static string ToPascalCase(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var cleaned = input.Replace("_", " ").Replace("-", " ");
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var titled = textInfo.ToTitleCase(cleaned.ToLowerInvariant());
            var sb = new StringBuilder();
            foreach (var ch in titled)
            {
                if (ch == ' ') continue;
                sb.Append(ch);
            }
            return sb.ToString();
        }

        // Convert a string to snake_case (safe for null/empty input)
        public static string ToSnakeCase(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TestKB.Models;

namespace TestKB.Services
{
    public class JsonToTextConverter
    {
        /// <summary>
        /// Converts JSON content (as a string) to a plain text representation 
        /// with proper encoding support for Turkish characters and escaping special characters.
        /// </summary>
        public static string ConvertJsonToText(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return string.Empty;

            List<ContentItem> items;
            try
            {
                // Configure JSON options with proper encoding settings
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };

                items = JsonSerializer.Deserialize<List<ContentItem>>(jsonContent, options)
                         ?? new List<ContentItem>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error converting JSON to text: {ex.Message}");
                return "Error reading content.";
            }

            var sb = new StringBuilder();

            // Ensure the StringBuilder has enough initial capacity
            sb.Capacity = jsonContent.Length;

            foreach (var item in items)
            {
                // Escape special characters in each field
                var category = EscapeSpecialCharacters(item.Category ?? string.Empty);
                var subCategory = EscapeSpecialCharacters(item.SubCategory ?? string.Empty);
                var content = EscapeSpecialCharacters(item.Content ?? string.Empty);

                sb.AppendLine($"Category: {category}");
                sb.AppendLine($"SubCategory: {subCategory}");
                sb.AppendLine("Content:");
                sb.AppendLine(content);
                sb.AppendLine(new string('-', 50));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes special characters in the input string to ensure safe text output
        /// </summary>
        private static string EscapeSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Handle common special characters that might need escaping
            // Replace with their escaped counterparts if needed
            var escaped = input
                .Replace("\\", "\\\\")  // Backslash
                .Replace("\"", "\\\"")  // Double quote
                .Replace("/", "\\/");   // Forward slash

            // Clean any control characters that might be present
            escaped = Regex.Replace(escaped, @"[\x00-\x1F]", string.Empty);

            return escaped;
        }
    }
}
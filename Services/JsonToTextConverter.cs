using System.Text;
using System.Text.Json;
using TestKB.Models;

namespace TestKB.Services
{
    public class JsonToTextConverter
    {
        /// <summary>
        /// Converts JSON content (as a string) to a plain text representation.
        /// </summary>
        public static string ConvertJsonToText(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return string.Empty;

            List<ContentItem> items;
            try
            {
                items = JsonSerializer.Deserialize<List<ContentItem>>(jsonContent)
                         ?? new List<ContentItem>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error converting JSON to text: {ex.Message}");
                return "Error reading content.";
            }

            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.AppendLine($"Category: {item.Category}");
                sb.AppendLine($"SubCategory: {item.SubCategory}");
                sb.AppendLine("Content:");
                sb.AppendLine(item.Content);
                sb.AppendLine(new string('-', 50));
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

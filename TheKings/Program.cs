using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TheKings
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        private static async Task Main(string[] args)
        {
            string gistData = "";
            // Get the gist data from provided gistID
            try
            {
                gistData = await GetGistDataAsync("10d65ccef9f29de3acd49d97ed423736");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // There is nothing to do if the API request has failed, close the application
                System.Environment.Exit(0);
            }

            // Convert the gist data to a usable format
            List<King> kings = null;
            try
            {
                kings = ConvertGistData(gistData) ?? throw new ArgumentNullException();
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("No usable data, closing the application");
                System.Environment.Exit(0);
            }

            // Display the gist data
            foreach (var king in kings)
            {
                Console.WriteLine(king);
            }
        }

        // Get gist data using Github API
        private static async Task<string> GetGistDataAsync(string gistId)
        {
            // Specify the request header
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Gist Test");

            // Fetch gist data
            var stringTask = client.GetStringAsync("https://api.github.com/gists/" + gistId);

            return await stringTask;
        }

        private static List<King> ConvertGistData(string gistData)
        {
            var kings = new List<King>();

            // Separate actual file contents from other Json data
            var contentsPattern = @",+\Wcontent\W:(\W\[\\n\s\s{.+}\\n\W)";
            Regex re = new Regex(contentsPattern);
            try
            {
                gistData = re.Match(gistData).Groups[1].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to convert the data\n" + e);
                return null;
            }

            // Remove extra characters
            gistData = gistData.Replace(@"\n", "").Replace(@"\", "");

            // Split into separate Json Object string for easy parsing
            // The Json serializer can't handle all of the data at once
            string[] splitGistData = Regex.Split(gistData, @"(?<=},)");

            // Trim the split data (trailing ,)
            for (int i = 0; i < splitGistData.Length; i++)
            {
                if (i == 0)
                {
                    splitGistData[i] = splitGistData[i].Remove(0, 4);
                    splitGistData[i] = splitGistData[i].Remove(splitGistData[i].Length - 1, 1);
                }
                else
                {
                    splitGistData[i] = splitGistData[i].Remove(0, 2);
                    splitGistData[i] = splitGistData[i].Remove(splitGistData[i].Length - 1, 1);
                }
            }

            // Deserialize the data and add to the list
            foreach (var data in splitGistData)
            {
                try
                {
                    kings.Add(JsonConvert.DeserializeObject<King>(data));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to deserialize {data}\n" + e);
                }
            }

            return kings;
        }
    }

    // Object representation of the Json data
    class King
    {
        [JsonPropertyName("id")] private string id { get; set; }

        [JsonPropertyName("name")] private string nm { get; set; }

        [JsonPropertyName("city")] private string cty { get; set; }

        [JsonPropertyName("house")] private string hse { get; set; }

        [JsonPropertyName("years")] private string yrs { get; set; }

        public override string ToString()
        {
            return $"Name: {nm} of {hse} \nCity: {cty} \nRuled in: {yrs}";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
            #region Get the gist data from provided gistID

            string gistData = "";

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

            #endregion

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

            // Process the kings data and answer questions
            AnswerQuestions(kings);
        }

        private static void AnswerQuestions(List<King> kings)
        {
            // Question 1
            Console.WriteLine("Q: How many monarchs are there in the list?");
            Console.WriteLine($"A: There are {kings.Count} monarchs in the list\n");
            // alternate answer using LinQ: kings.Last().id

            // Question 2
            Console.WriteLine("Q: Which monarch ruled the longest (and for how long)?");

            // Get longest ruling monarch
            var longestRulingMonarch = kings.Aggregate((x1, x2) => x1.GetRuleTime() > x2.GetRuleTime() ? x1 : x2);

            Console.WriteLine(
                $"A: The longest ruler was/is {longestRulingMonarch.nm} who ruled for {longestRulingMonarch.GetRuleTime()} years\n");

            // Question 3
            Console.WriteLine("Q: Which house ruled the longest (and for how long)?");

            var houses = new Dictionary<string, int>();

            // Populate a dictionary with house data
            foreach (var king in kings)
            {
                if (!houses.ContainsKey(king.hse))
                {
                    houses.Add(king.hse, king.GetRuleTime());
                }
                else
                {
                    int newValue;
                    houses.TryGetValue(king.hse, out newValue);
                    houses[king.hse] = newValue + king.GetRuleTime();
                }
            }

            // Get house name with most years ruled
            var bestHouse = houses.Aggregate((x1, x2) => x1.Value > x2.Value ? x1 : x2);

            Console.WriteLine($"A: Longest Ruling house is {bestHouse.Key}, which ruled for {bestHouse.Value} years\n");

            // Question 4
            Console.WriteLine("Q: What was the most common first name?");

            var names = new Dictionary<string, int>();

            // Populate the dictionary with the monarch names
            foreach (var king in kings)
            {
                var name = king.nm.Split(" ")[0];
                if (!names.ContainsKey(name))
                {
                    names.Add(name, 1);
                }
                else
                {
                    int newValue;
                    names.TryGetValue(name, out newValue);
                    names[name] = newValue + 1;
                }
            }

            // Get the most used name
            var mostCommonName = names.Aggregate((x1, x2) => x1.Value > x2.Value ? x1 : x2);

            Console.WriteLine($"A: The most common name was {mostCommonName.Key}\n");
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
        [JsonPropertyName("id")] public string id { get; set; }

        [JsonPropertyName("name")] public string nm { get; set; }

        [JsonPropertyName("city")] public string cty { get; set; }

        [JsonPropertyName("house")] public string hse { get; set; }

        [JsonPropertyName("years")] public string yrs { get; set; }

        public override string ToString()
        {
            return $"Name: {nm} of {hse} \nCity: {cty} \nRuled in: {yrs}";
        }

        // Calculate and return how long given king has ruled for
        public int GetRuleTime()
        {
            // if there is no dask I assume given King has ruled for one year
            if (!yrs.Contains("-"))
            {
                return 1;
            }
            else
            {
                // If there is only start year and a dash, split throws an exception. I assume those rules are in power until today
                try
                {
                    var years = yrs.Split('-');

                    return int.Parse(years[1]) - int.Parse(years[0]);
                }
                catch (FormatException)
                {
                    return DateTime.Today.Year - int.Parse(yrs.Replace("-", ""));
                }
            }
        }
    }
}
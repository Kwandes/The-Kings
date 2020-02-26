using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TheKings
{
    class Program
    {
        private static readonly HttpClient Client = new HttpClient();

        private static async Task Main()
        {
            // Get the gist data from provided gistID
            var gistData =
                await GetGistDataAsync(
                    "https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048");

            // Convert the gist data to a usable format

            var kings = await JsonSerializer.DeserializeAsync<IEnumerable<King>>(gistData);

            // Check for 
            if (kings == null)
            {
                Console.WriteLine("No data, closing the application");
                Environment.Exit(0);
            }

            // Process the kings data and answer questions
            AnswerQuestions(kings);
        }

        private static void AnswerQuestions(IEnumerable<King> kings)
        {
            // Question 1
            Console.WriteLine("Q: How many monarchs are there in the list?");
            Console.WriteLine($"A: There are {kings.Count()} monarchs in the list\n");
            // alternate answer using LinQ: kings.Last().id

            // Question 2
            Console.WriteLine("Q: Which monarch ruled the longest (and for how long)?");

            // Get longest ruling monarch
            var longestRulingMonarch = kings.Aggregate((x1, x2) => x1.YearsInReign > x2.YearsInReign ? x1 : x2);

            Console.WriteLine(
                $"A: The longest ruler was/is {longestRulingMonarch.Name} who ruled for {longestRulingMonarch.YearsInReign} years\n");

            // Question 3
            Console.WriteLine("Q: Which house ruled the longest (and for how long)?");

            var houses = new Dictionary<string, int>();

            // Populate a dictionary with house data
            foreach (var king in kings)
            {
                if (!houses.ContainsKey(king.House))
                {
                    houses.Add(king.House, king.YearsInReign);
                }
                else
                {
                    int newValue;
                    houses.TryGetValue(king.House, out newValue);
                    houses[king.House] = newValue + king.YearsInReign;
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
                var name = king.Name.Split(" ")[0];
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
        private static async Task<Stream> GetGistDataAsync(string gistLink)
        {
            // Specify the request header
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            Client.DefaultRequestHeaders.Add("User-Agent", ".NET Gist Test");

            // Fetch gist data
            var streamTask = await Client.GetStreamAsync(gistLink);
            return streamTask;
        }
    }

    // Object representation of the Json data
    public class King
    {
        private int _yearsInReign;
        private string _datesRuled;

        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("nm")] public string Name { get; set; }

        [JsonPropertyName("cty")] public string City { get; set; }

        [JsonPropertyName("hse")] public string House { get; set; }

        [JsonPropertyName("yrs")]
        public string DatesRuled
        {
            get => _datesRuled;
            set
            {
                _datesRuled = value;
                // Single date, assume to be 1 year
                if (!_datesRuled.Contains('-'))
                {
                    _yearsInReign = 1;
                }
                // The date consists of 2 years
                else if (!_datesRuled.EndsWith('-'))
                {
                    var years = _datesRuled.Split('-');
                    _yearsInReign = int.Parse(years[1]) - int.Parse(years[0]);
                }
                // The date is one year but with a dash, remove the dash and subtract from current Date
                else
                {
                    _yearsInReign = DateTime.Today.Year - int.Parse(_datesRuled.TrimEnd('-'));
                }
            }
        }

        public int YearsInReign => _yearsInReign;

        public override string ToString()
        {
            return $"Name: {Name} of {House} \nCity: {City} \nRuled in: {DatesRuled}";
        }
    }
}
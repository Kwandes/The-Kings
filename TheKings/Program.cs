using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
            
            // handle the gist data

            // Display the gist data
            Console.WriteLine(gistData);
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
    }
}
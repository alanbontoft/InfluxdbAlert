using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using InfluxDB3.Client;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

class Program
{
    private static readonly HttpClient http = new HttpClient();

    private const string InfluxHost = "http://192.168.68.90:8181";

    private const string InfluxOrg = "";

    private const string InfluxDb = "HomeDatabase";
    private const string Webhook = "http://localhost:7777/";

    static async Task Main()
    {
        var sw = new Stopwatch();

        sw.Restart();

        string InfluxToken = Environment.GetEnvironmentVariable("INFLUXDB3_TOKEN");

        var client = new InfluxDBClient(InfluxHost, InfluxToken, InfluxOrg, InfluxDb);

        sw.Stop();
        Console.WriteLine($"InfluxDB client created in {sw.ElapsedMilliseconds} ms");

        // Load alert rules
        var json = File.ReadAllText("alerts.json");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var alerts = JsonSerializer.Deserialize<AlertRule[]>(json, options);


        while (true)
        {

            foreach (var alert in alerts)
            {
                var result = client.Query(alert.Query);

                await foreach (var rows in result)
                {

                    // -------------------------
                    // Deadman alert
                    // -------------------------
                    if (alert.Type == "deadman")
                    {
                        // if (rows.Length == 0)
                        // {
                        //     await SendTeamsAlert(alert.Name, "No data returned at all");
                        //     continue;
                        // }

                        // var lastTime = DateTime.Parse(rows[0].ToString());
                        // var ageSeconds = (DateTime.UtcNow - lastTime).TotalSeconds;

                        // if (ageSeconds > alert.Max_Age_Seconds)
                        // {
                        //     await SendTeamsAlert(
                        //         alert.Name,
                        //         $"No data for {ageSeconds:F0} seconds (limit {alert.Max_Age_Seconds})"
                        //     );
                        // }

                        continue;
                    }

                    // -------------------------
                    // Threshold alert
                    // -------------------------
                    if (alert.Type == "threshold")
                    {
                        if (rows.Length == 0)
                            continue;

                        double value = Convert.ToDouble(rows[0]);

                        var epoc = (rows[1] as BigInteger?) / 1000;

                        // InfluxDB 3 returns timestamps in microseconds, we need to convert to seconds
                        epoc /= 1000000;

                        var time = DateTimeOffset.FromUnixTimeSeconds(long.Parse(epoc.ToString())).DateTime;

                        bool triggered =
                            (alert.Comparison == "greater" && value > alert.Threshold) ||
                            (alert.Comparison == "less" && value < alert.Threshold);

                        if (triggered)
                        {
                            await SendTeamsAlert(alert.Name, value, $"{time:dd-MM-yyyy HH:mm:ss}");

                            var emailresult = SendEmailAsync(new string[] { "joe.blogs@hotmail.co.uk", "john.doe@virginmedia.com" }, $"Alert: {alert.Name}", $"Value: {value}, Timestamp: {time:dd-MM-yyyy HH:mm:ss}");

                            await emailresult;
                        }
                    }
                }
            }

            Task.Delay(5000).Wait();
        }
    }

    private static async Task SendTeamsAlert(string title, double value, string timestamp)
    {
        var payload = new
        {
            title = $"{title}",
            value = value,
            timestamp = timestamp
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await http.PostAsync(Webhook, content);
    }


    private static async Task<string> SendEmailAsync(string[] to, string subject, string bodyText, string? bodyHtml = null)
    {
        var key = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        var client = new AmazonSimpleEmailServiceClient(
            key,
            secret,
            RegionEndpoint.EUNorth1 // choose your SES region 
            );

        var request = new SendEmailRequest
        {
            Source = "mike.smith@gmail.com",
            Destination = new Destination
            {
                ToAddresses = new List<string>(to)
            },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Text = new Content(bodyText),
                    Html = bodyHtml is not null ? new Content(bodyHtml) : null
                }
            }
        };

        var response = await client.SendEmailAsync(request);
        return response.MessageId;
    }
}

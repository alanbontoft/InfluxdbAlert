using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class AlertMessage
{
    public string? Message { get; set; }

    public string? Title { get; set; }

    public double Value { get; set; }
}

class Program
{
    private static readonly HttpClient http = new HttpClient();

    private const string InfluxHost = "YOUR_INFLUXDB3_HOST";
    private const string InfluxToken = "YOUR_TOKEN";
    private const string InfluxOrg = "YOUR_ORG";
    private const string TeamsWebhook = "http://localhost:5000/";

    private const string WebhookSite = "https://webhook.site/73304434-6b8f-4e0a-b2f0-b24cdffa2898";

    static async Task Main()
    {

        var count = 0.0;
        await Task.Run(async () =>
        {
            while (true)
            {
                SendTeamsAlert("Alert", "Run Time", count);
                await Task.Delay(5300);
                count += 5.3;
            }

        });

    }

    private static async Task SendTeamsAlert(string title, string message, double value)
    {
        //var payload = new
        //{
        //    text = $"🚨 **{title}**\n{message}",

        //};

        AlertMessage payload = new() { Title = title, Message = message, Value = value };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await http.PostAsync(WebhookSite, content);
    }
}

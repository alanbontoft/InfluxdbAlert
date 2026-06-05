using System.Net;
using System.Text;

const string url = "http://localhost:7777/";
var listener = new HttpListener();
listener.Prefixes.Add(url);
listener.Start();

Console.WriteLine($"Listening on {url}");
Console.WriteLine("Waiting for POST / ...");

while (true)
{
    var context = listener.GetContext();
    var request = context.Request;
    var response = context.Response;

    if (request.HttpMethod == "POST")
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = reader.ReadToEnd();

        Console.WriteLine("Received JSON:");
        Console.WriteLine(body);
    }

    var buffer = Encoding.UTF8.GetBytes("{\"status\":\"received\"}");
    response.ContentType = "application/json";
    response.OutputStream.Write(buffer, 0, buffer.Length);
    response.Close();
}

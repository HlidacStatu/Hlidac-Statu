var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run(async context =>
{
    // Print the request path
    Console.WriteLine($"=== Path: \n{context.Request.Path}");

    // Print all the headers
    foreach (var header in context.Request.Headers)
    {
        Console.WriteLine($"=== Header: \n{header.Key} = {header.Value}");
    }

    // Print query parameters
    foreach (var queryParam in context.Request.Query)
    {
        Console.WriteLine($"=== Query: \n{queryParam.Key} = {queryParam.Value}");
    }

    // Print request body if any
    if (context.Request.ContentLength > 0)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        Console.WriteLine($"=== \nBody: {body}");
    }

    // Send a response
    await context.Response.WriteAsync("Request processed. Check the console output.");
});

app.Run();
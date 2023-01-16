using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

const string RateLimitingPolicyName = "user";
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(RateLimitingPolicyName, opt =>
    {
        opt.Window = TimeSpan.FromSeconds(5);
        opt.PermitLimit = 1;
        // opt.QueueLimit = 1;
    });
    
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.Add(HeaderNames.RetryAfter, retryAfter.ToString());
        }
        return new ValueTask();
    };
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/user", context => CreateResponse(context, "Hello User"))
    .RequireRateLimiting(RateLimitingPolicyName);

app.MapGet("/admin", context => CreateResponse(context, "Hello Admin"));

app.Run();

Task CreateResponse(HttpContext httpContext, string message)
{
    Console.WriteLine($"timestamp : {DateTime.Now:MM/dd/yyyy hh:mm:ss.fff tt}");
    return httpContext.Response.WriteAsJsonAsync(new Response
    {
        Message = message
    });
}

public record Response
{
    public DateTime CreatedAt { get; } = DateTime.Now;
    public string Message { get; init; }
}
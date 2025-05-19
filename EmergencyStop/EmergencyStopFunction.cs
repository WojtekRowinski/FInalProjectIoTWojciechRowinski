using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EmergencyStop;

public class EmergencyStopFunction
{
    private readonly ILogger<EmergencyStopFunction> _logger;

    public EmergencyStopFunction(ILogger<EmergencyStopFunction> logger)
    {
        _logger = logger;
    }

    [Function("EmergencyStopFunction")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
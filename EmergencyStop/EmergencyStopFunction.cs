using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class EmergencyStopFunction
{
    private readonly ILogger<EmergencyStopFunction> _logger;
    private readonly string _iotHubConnectionString;

    public EmergencyStopFunction(ILogger<EmergencyStopFunction> logger)
    {
        _logger = logger;
        _iotHubConnectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
    }

    [Function("EmergencyStopFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("EmergencyStopFunction triggered");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(requestBody);

        string deviceId = data[0]?.deviceId;

        if (string.IsNullOrEmpty(deviceId))
        {

            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("deviceId is required");
            return badResponse;
        }
        
        

        if (deviceId == "Device 1")
        {
                //IOT HUB device name
            deviceId = "Production01"; 
        }


        using var serviceClient = ServiceClient.CreateFromConnectionString(_iotHubConnectionString);

        var methodInvocation = new CloudToDeviceMethod("EmergencyStop")
        {
            ResponseTimeout = TimeSpan.FromSeconds(30)
        };
        methodInvocation.SetPayloadJson("{}");

            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
            _logger.LogInformation($"EmergencyStop sent to {deviceId}, status: {response.Status}");

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await okResponse.WriteStringAsync($"EmergencyStop invoked on device {deviceId}");
            return okResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error invoking EmergencyStop: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Error invoking EmergencyStop");
            return errorResponse;
        }
    }
}

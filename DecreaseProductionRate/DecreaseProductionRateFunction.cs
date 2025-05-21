using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class DecreaseProductionRateFunction
{
    private readonly ILogger<DecreaseProductionRateFunction> _logger;
    private readonly string _iotHubConnectionString;

    public DecreaseProductionRateFunction(ILogger<DecreaseProductionRateFunction> logger)
    {
        _logger = logger;
        _iotHubConnectionString = Environment.GetEnvironmentVariable("IotHubConnectionString");
    }

    [Function("DecreaseProductionRateFunction")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("DecreaseProductionRateFunction triggered");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        JsonElement data = JsonSerializer.Deserialize<JsonElement>(requestBody);

        string deviceId = data[0].GetProperty("deviceId").GetString();

        if (deviceId == "Device 1")
        {
            deviceId = "Production01"; // mapowanie na rzeczywisty deviceId
        }

        var registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);
        var twin = await registryManager.GetTwinAsync(deviceId);

        _logger.LogInformation($"Twin for device {deviceId}:\nReported: {twin.Properties.Reported.ToJson()}");

        // Pobierz aktualny productionRate z reported
        int currentRate = Convert.ToInt32(twin.Properties.Reported["productionRate"]);
        int newRate = Math.Max(0, currentRate - 10);

        // Ustaw nowy rate w desired
        var patch = new TwinCollection();
        patch["productionRate"] = newRate;

        await registryManager.UpdateTwinAsync(deviceId, new Twin { Properties = { Desired = patch } }, twin.ETag);
        _logger.LogInformation($"Updated desired productionRate to {newRate} for device {deviceId}");

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync($"productionRate updated to {newRate} for device {deviceId}");
        return response;
    }
}

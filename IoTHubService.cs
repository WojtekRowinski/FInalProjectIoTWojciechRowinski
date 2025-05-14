using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

public class IoTHubService
{
    private DeviceClient _deviceClient;
    private string _connectionString;
    private DeviceErrorFlags _lastDeviceErrors = DeviceErrorFlags.None; // Initialize to None

    public IoTHubService(string connectionString)
    {
        _connectionString = connectionString;
        _deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
    }

    private readonly OpcUaService _opcUaService;

    public IoTHubService(string connectionString, OpcUaService opcUaService)
    {
        _connectionString = connectionString;
        _deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
        _opcUaService = opcUaService;
    }


    public async Task SendTelemetryDataAsync(TelemetryData telemetryData)
    {
        try
        {
            // Cast DeviceErrors object to DeviceErrorFlags enum
            DeviceErrorFlags currentError = (DeviceErrorFlags) (int) telemetryData.DeviceError.Value;

            // 1. Send main telemetry message
            var telemetryJson = $"{{\"deviceId\":\"{telemetryData.DeviceId.Value}\",\"timestamp\":\"{telemetryData.Timestamp:O}\",\"productionStatus\":\"{telemetryData.ProductionStatus.Value}\",\"workorderId\":\"{telemetryData.WorkorderId.Value}\",\"productionRate\":{telemetryData.ProductionRate.Value},\"goodCount\":{telemetryData.GoodCount.Value},\"badCount\":{telemetryData.BadCount.Value},\"temperature\":{telemetryData.Temperature.Value.ToString().Replace(',', '.')},\"deviceErrors\":{(int)currentError}}}";

            var message = new Message(Encoding.UTF8.GetBytes(telemetryJson));
            await _deviceClient.SendEventAsync(message);
            Console.WriteLine("Dane telemetryczne zostały wysłane do IoT Hub.");

            // 2. Handle DeviceErrors change and send D2C event + update device twin
            if (currentError != _lastDeviceErrors)
            {
                _lastDeviceErrors = currentError;

                // Generate error messages based on the flags
                var errorMessages = GetErrorMessages(currentError);

                // Event DeviceErrorsChanged
                var errorEvent = new Message(Encoding.UTF8.GetBytes($"{{\"event\":\"DeviceErrorsChanged\",\"deviceErrors\":{(int)currentError},\"errorMessages\":\"{errorMessages}\"}}"));
                await _deviceClient.SendEventAsync(errorEvent);
                Console.WriteLine("Błędy urządzenia zmieniono – wysłano zdarzenie.");

                // Reported Device Twin update
                var reportedProperties = new TwinCollection();
                reportedProperties["deviceErrors"] = (int)currentError;
                await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
                Console.WriteLine("Zaktualizowano Device Twin (reported).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wysyłania danych: {ex.Message}");
        }
    }

    private string GetErrorMessages(DeviceErrorFlags currentErrors)
    {
        var errorMessages = new StringBuilder();

        if (currentErrors.HasFlag(DeviceErrorFlags.EmergencyStop))
            errorMessages.Append("Emergency Stop, ");
        if (currentErrors.HasFlag(DeviceErrorFlags.PowerFailure))
            errorMessages.Append("Power Failure, ");
        if (currentErrors.HasFlag(DeviceErrorFlags.SensorFailure))
            errorMessages.Append("Sensor Failure, ");
        if (currentErrors.HasFlag(DeviceErrorFlags.Unknown))
            errorMessages.Append("Unknown Error, ");

        // Remove trailing comma and space
        if (errorMessages.Length > 0)
            errorMessages.Remove(errorMessages.Length - 2, 2);

        return errorMessages.ToString();
    }
    private async Task<MethodResponse> EmergencyStopMethod(MethodRequest methodRequest, object userContext)
    {
        try
        {
            string productionStatusNodeId = "ns=2;s=Device 1/ProductionStatus"; // Replace with your actual node ID
            _opcUaService.WriteNode(productionStatusNodeId, 0); // Set production status to 0

            Console.WriteLine("Produkcja została zatrzymana (Emergency Stop).");
            string responseJson = "{\"result\":\"Produkcja została zatrzymana (Emergency Stop)\"}";
            return new MethodResponse(Encoding.UTF8.GetBytes(responseJson), 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zatrzymywania produkcji (Emergency Stop): {ex.Message}");
            string responseJson = $"{{\"error\":\"{ex.Message}\"}}";
            return new MethodResponse(Encoding.UTF8.GetBytes(responseJson), 500);
        }
    }

    public async Task RegisterDirectMethodHandlersAsync()
    {
        await _deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopMethod, null);
        Console.WriteLine("Zarejestrowano metodę bezpośrednią: EmergencyStop.");
    }

}


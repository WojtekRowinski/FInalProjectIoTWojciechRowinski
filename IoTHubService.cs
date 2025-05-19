using Opc.UaFx;
using Opc.UaFx.Client;
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
    private DeviceErrorFlags _lastDeviceErrors = DeviceErrorFlags.None; 

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
            
            DeviceErrorFlags currentError = (DeviceErrorFlags) (int) telemetryData.DeviceError.Value;

            
            var telemetryJson = $"{{\"deviceId\":\"{telemetryData.DeviceId.Value}\",\"timestamp\":\"{telemetryData.Timestamp:O}\",\"productionStatus\":\"{telemetryData.ProductionStatus.Value}\",\"workorderId\":\"{telemetryData.WorkorderId.Value}\",\"productionRate\":{telemetryData.ProductionRate.Value},\"goodCount\":{telemetryData.GoodCount.Value},\"badCount\":{telemetryData.BadCount.Value},\"temperature\":{telemetryData.Temperature.Value.ToString().Replace(',', '.')},\"deviceErrors\":{(int)currentError}}}";

            var message = new Message(Encoding.UTF8.GetBytes(telemetryJson));
            await _deviceClient.SendEventAsync(message);
            Console.WriteLine("Dane telemetryczne zostały wysłane do IoT Hub.");

            
            if (currentError != _lastDeviceErrors)
            {
                _lastDeviceErrors = currentError;

                var errorMessages = GetErrorMessages(currentError);

                
                var errorEvent = new Message(Encoding.UTF8.GetBytes($"{{\"event\":\"DeviceErrorsChanged\",\"deviceErrors\":{(int)currentError},\"errorMessages\":\"{errorMessages}\"}}"));
                await _deviceClient.SendEventAsync(errorEvent);
                Console.WriteLine("Błędy urządzenia zmieniono – wysłano zdarzenie.");

                
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

      
        if (currentErrors.HasFlag(DeviceErrorFlags.PowerFailure))
            errorMessages.Append("Power Failure, ");
        if (currentErrors.HasFlag(DeviceErrorFlags.SensorFailure))
            errorMessages.Append("Sensor Failure, ");
        if (currentErrors.HasFlag(DeviceErrorFlags.Unknown))
            errorMessages.Append("Unknown Error, ");

        
        if (errorMessages.Length > 0)
            errorMessages.Remove(errorMessages.Length - 2, 2);

        return errorMessages.ToString();
    }
    public async Task RegisterDirectMethodHandlersAsync()
    {
        await _deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopMethod, null);
        Console.WriteLine("Zarejestrowano metodę bezpośrednią: EmergencyStop.");
    }

    private async Task<MethodResponse> EmergencyStopMethod(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine("EmergencyStop method invoked!");

        try
        {
            if (_opcUaService != null)
            {
                _opcUaService.EmergencyStop();
                Console.WriteLine("Production status set to 'Stopped' on OPC UA server.");
            }

            Console.WriteLine("EmergencyStop event sent to IoT Hub.");
            var reportedProperties = new TwinCollection();
            reportedProperties["productionStatus"] = 0;
            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            Console.WriteLine("Updated Device Twin (reported) with productionStatus = 0");

            return new MethodResponse(200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during EmergencyStop: {ex.Message}");
            return new MethodResponse(500);
        }
    }


}


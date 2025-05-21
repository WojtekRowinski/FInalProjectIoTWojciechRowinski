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
                Console.WriteLine("Zaktualizowano Device Twin (reported) - DeviceError.");
            }

            
            var productionRateProperties = new TwinCollection();
            productionRateProperties["productionRate"] = telemetryData.ProductionRate.Value;
            await _deviceClient.UpdateReportedPropertiesAsync(productionRateProperties);
            Console.WriteLine("Zaktualizowano Device Twin (reported) - ProductionRate.");

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

    private async Task<MethodResponse> EmergencyStopMethod(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine("Metoda EmergencyStop została wywołana.");

        try
        {
            if (_opcUaService != null)
            {
                _opcUaService.EmergencyStop();
                Console.WriteLine("Status produkcji ustawiony na „Zatrzymano” na serwerze OPC UA.");
            }
           

            return new MethodResponse(200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wykonywania EmergencyStop: {ex.Message}");
            return new MethodResponse(500);
        }
    }
    private async Task<MethodResponse> ResetErrorStatusMethod(MethodRequest methodRequest, object userContext)
    {
        Console.WriteLine("Metoda ResetErrorStatus została wywołana.");

        try
        {
            if (_opcUaService != null)
            {
                _opcUaService.ResetErrorStatus();
                Console.WriteLine("Device Error zostało usatwione na: None");
            }

            return new MethodResponse(200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas wykonywania ResetErrorStatus: {ex.Message}");
            return new MethodResponse(500);
        }
    }

    public async Task MonitorDesiredPropertiesAsync()
    {
        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (desiredProperties, context) =>
        {
            if (desiredProperties.Contains("productionRate"))
            {
                int newRate = (int)desiredProperties["productionRate"];
                Console.WriteLine($"Odebrano desired productionRate: {newRate}");

                // Zapisz do OPC UA
                _opcUaService?.WriteNode("ns=2;s=Device 1/ProductionRate", newRate);

                // Aktualizuj reported, że nowy rate został ustawiony
                var reported = new TwinCollection();
                reported["productionRate"] = newRate;
                await _deviceClient.UpdateReportedPropertiesAsync(reported);
                Console.WriteLine($"Zaktualizowano reported productionRate: {newRate}");
            }
        }, null);
    }

    public async Task RegisterDirectMethodHandlersAsync()
    {
        await _deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopMethod, null);
        await _deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusMethod, null);
        Console.WriteLine("Zarejestrowano metody bezpośrednie: EmergencyStop i ResetErrorStatus.");
    }


}


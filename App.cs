using System;
using System.IO;
using System.Threading.Tasks;

class App
{
    static async Task Main(string[] args)
    {
        //keys.txt musi być skopiowane i wklejone do bin\Debug\net8.0
        string[] config = File.ReadAllLines(".\\keys.txt");
        // Adres OPC UA serwera
        string serverUrl = config[1]; 

        // Połączenie z OPC UA
        var opcUaService = new OpcUaService(serverUrl);

        // Konfiguracja IoT Hub
        string connectionString = config[0];
        var iotHubService = new IoTHubService(connectionString, opcUaService);
        await iotHubService.RegisterDirectMethodHandlersAsync();
        await iotHubService.MonitorDesiredPropertiesAsync();

        string blobConnectionString = config[2];
        string containerName = "telemetrydata";
        var blobService = new AzureBlobStorageService(blobConnectionString, containerName);

        // Zdefiniuj ścieżkę do węzłów OPC UA
        string baseNodePath = "ns=2;s=Device 1";
        
        // Odczyt danych telemetrycznych
           while (true)
        {
            var telemetryData = new TelemetryData
            {
                DeviceId = "Device 1",  
                Timestamp = DateTime.UtcNow,
                ProductionStatus = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/ProductionStatus"),
                WorkorderId = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/WorkorderId"),
                ProductionRate = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/ProductionRate"),
                GoodCount = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/GoodCount"),
                BadCount = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/BadCount"),
                Temperature = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/Temperature"),
                DeviceError = (Opc.UaFx.OpcValue) opcUaService.ReadNode($"{baseNodePath}/DeviceError") 
            };

            // Wysłanie telemetrii do Azure IoT Hub
            await iotHubService.SendTelemetryDataAsync(telemetryData);
            Console.WriteLine($"Wysłano dane telemetryczne o {telemetryData.Timestamp}");

            await blobService.SaveTelemetryDataAsync(telemetryData);

            Console.WriteLine($"Wysłano dane i zapisano do Blob Storage: {telemetryData.Timestamp}");
            await Task.Delay(1000);
           
        }

       
        
        Console.WriteLine("Naciśnij dowolny klawisz, aby zakończyć.");
        Console.ReadKey();
    }

    
}

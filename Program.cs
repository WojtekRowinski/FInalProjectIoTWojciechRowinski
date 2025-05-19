using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Adres OPC UA serwera
        string serverUrl = "opc.tcp://localhost:4840"; 

        // Połączenie z OPC UA
        var opcUaService = new OpcUaService(serverUrl);

        // Konfiguracja IoT Hub
        string connectionString = "HostName=IoHubProject.azure-devices.net;DeviceId=Production01;SharedAccessKey=D1SDt/NCzVWYtrHGlooytInNREyNgmp5EfcZ1IeXZnc=";
        var iotHubService = new IoTHubService(connectionString);

        string blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=projektwr;AccountKey=pBozxArquw11EuvOT9kYG4wOj6cNFsw1C8QubgjcUJop4cJjcbq5z3MBG3lXAX0W5TB2XULpZqsC+AStNkdrgg==;EndpointSuffix=core.windows.net";
        string containerName = "telemetrydata";
        var blobService = new AzureBlobStorageService(blobConnectionString, containerName);

        // Zdefiniuj ścieżkę do węzłów OPC UA
        string baseNodePath = "ns=2;s=Device 1"; 

        // Odczyt danych telemetrycznych
        int i = 0;
        //while (i <= 1)
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
            i++;
        }

       
        await iotHubService.RegisterDirectMethodHandlersAsync();
        // Poczekaj na zakończenie
        Console.WriteLine("Naciśnij dowolny klawisz, aby zakończyć.");
        Console.ReadKey();
    }

    
}

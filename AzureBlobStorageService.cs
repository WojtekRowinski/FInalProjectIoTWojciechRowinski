using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

public class AzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        // Tworzenie klienta kontenera blobów
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task SaveTelemetryDataAsync(TelemetryData telemetryData)
    {
        DeviceErrorFlags currentError = (DeviceErrorFlags)(int)telemetryData.DeviceError.Value;

        var telemetryJson = $"{{\"deviceId\":\"{telemetryData.DeviceId.Value}\",\"timestamp\":\"{telemetryData.Timestamp:O}\",\"productionStatus\":\"{telemetryData.ProductionStatus.Value}\",\"workorderId\":\"{telemetryData.WorkorderId.Value}\",\"productionRate\":{telemetryData.ProductionRate.Value},\"goodCount\":{telemetryData.GoodCount.Value},\"badCount\":{telemetryData.BadCount.Value},\"temperature\":{telemetryData.Temperature.Value.ToString().Replace(',', '.')},\"deviceErrors\":{(int)currentError}}}";
      

        // Generowanie unikalnej nazwy pliku na podstawie DeviceId i Timestamp
        string blobName = $"{telemetryData.DeviceId.Value}/{telemetryData.Timestamp:yyyyMMdd_HHmmss}.json";

        // Uzyskiwanie klienta bloba
        BlobClient blobClient = _containerClient.GetBlobClient(blobName);

        // Przesyłanie danych do bloba
        using (var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(telemetryJson)))
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        Console.WriteLine($"Dane telemetryczne zapisane do bloba: {blobName}");
    }
}

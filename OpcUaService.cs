using Opc.UaFx;
using Opc.UaFx.Client;
using System;

public class OpcUaService
{
    private OpcClient _client;

    public OpcUaService(string serverUrl)
    {
        // Inicjalizacja klienta OPC UA
        _client = new OpcClient(serverUrl);
    }

    // Metoda do odczytu wartości węzła
    public object ReadNode(string nodeId)
    {
        try
        {
            _client.Connect();
            // Odczyt wartości węzła
            var nodeValue = _client.ReadNode(nodeId);

            if (nodeValue != null)
            {
                return nodeValue;
            }
            else
            {
                Console.WriteLine($"Brak danych w węźle {nodeId}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd odczytu węzła {nodeId}: {ex.StackTrace}");
            return null;
        }
        finally { _client.Disconnect(); }
    }
    public void WriteNode(string nodeId, object value)
    {
        try
        {
            _client.Connect();
            _client.WriteNode(nodeId, value);
            Console.WriteLine($"Wartość węzła '{nodeId}' została ustawiona na {value}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu do węzła '{nodeId}': {ex.Message}");
        }
        finally
        {
            _client.Disconnect();
        }
    }

}

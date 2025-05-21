using Opc.Ua;
using Opc.Ua.Client;
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
    public void EmergencyStop()
    {
        try
        {
            _client.Connect();
            _client.CallMethod("ns=2;s=Device 1", "ns=2;s=Device 1/EmergencyStop");
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu węzła {ex.Message}");
        }
        finally
        {
            _client.Disconnect();
        }
    }
    public void ResetErrorStatus()
    {
        try
        {
            _client.Connect();
            _client.CallMethod("ns=2;s=Device 1", "ns=2;s=Device 1/ResetErrorStatus");
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu węzła {ex.Message}");
        }
        finally
        {
            _client.Disconnect();
        }
    }
    public void WriteNode(string nodeId, object value)
    {
        try
        {
            _client.Connect();
            _client.WriteNode(nodeId, value);
            Console.WriteLine($"Wartość {value} została zapisana do węzła {nodeId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas zapisu do węzła {nodeId}: {ex.Message}");
        }
        finally
        {
            _client.Disconnect();
        }
    }

}






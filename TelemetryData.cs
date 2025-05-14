public class TelemetryData
{
    public Opc.UaFx.OpcValue DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public Opc.UaFx.OpcValue ProductionStatus { get; set; }
    public Opc.UaFx.OpcValue WorkorderId { get; set; }
    public Opc.UaFx.OpcValue ProductionRate { get; set; }
    public Opc.UaFx.OpcValue GoodCount { get; set; }
    public Opc.UaFx.OpcValue BadCount { get; set; }
    public Opc.UaFx.OpcValue Temperature { get; set; }

    public Opc.UaFx.OpcValue DeviceError { get; set; } // Keep as object to ensure compatibility
}
[Flags]
public enum DeviceErrorFlags
{
    None = 0,
    EmergencyStop = 1,
    PowerFailure = 2,
    SensorFailure = 4,
    Unknown = 8
}
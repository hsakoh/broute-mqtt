namespace BRouteController;

public class BRouteOptions
{
    public string Id { get; set; } = default!;

    public string Pw { get; set; } = default!;

    public string SerialPort { get; set; } = default!;

    public bool ForcePANScan { get; set; } = false;
    public string PanDescSavePath { get; set; } = "/data/EPANDESC.json";

    public TimeSpan InstantaneousValueInterval { get; set; } = TimeSpan.FromMinutes(5);

}

namespace HelloServer;

public class RoundStateMessage
{
    public string Type { get; set; } = "round_state";
    
    public float RemainingTime { get; set; }
    public float DurabilityRatio { get; set; }
    public float SpeedRatio { get; set; }
    public int Cleared { get; set; }
    public int Total { get; set; }
    public string Phase { get; set; }
    public string Gear { get; set; }
}
namespace HelloServer.Traffic;

public class TrafficAgentState
{
    public string Id { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float Rotation { get; set; }
}

public class TrafficStateMessage
{
    public string Type { get; set; } = "traffic_state";

    public int SceneVersion { get; set; }
    public TrafficAgentState[] States { get; set; }
}
namespace HelloServer;

#region 클라이언트 -> 서버 (C2S)

public class MinigameClientReadyMessage
{
    public string Type { get; set; } = "minigame_client_ready";
}

public class MinigameStartRequestMessage
{
    public string Type { get; set; } = "minigame_start_request";
    public string MiniGameType { get; set; }
}

public class BarricadeFuseClickMessage
{
    public string Type { get; set; } = "barricade_fuse_click";
    public int FuseIndex { get; set; }
}

#endregion

#region 서버 -> 클라이언트 (S2C)

public class MinigameStartedMessage
{
    public string Type { get; set; } = "minigame_started";
    public string MiniGameType { get; set; }
    public string OperatorId { get; set; }
}

public class FuelStateMessage
{
    public string Type { get; set; } = "fuel_state";
    public float GaugePercent { get; set; }
    public bool IsPressed { get; set; }
}

public class BarricadeProgressMessage
{
    public string Type { get; set; } = "barricade_progress";
    public int FuseIndex { get; set; }
}

#endregion
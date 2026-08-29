namespace HelloServer;

// 클라이언트 -> 서버 -> 호스트
public class VehicleInputMessage
{
    public string Type { get; set; } = "vehicle_input";

    public string UserId { get; set; }

    public float Steer { get; set; }
    public float Accel { get; set; }
    public float Brake { get; set; }
    public int Gear { get; set; }
}

// 호스트 -> 서버 -> 나머지 클라이언트
public class VehicleStateMessage
{
    public string Type { get; set; } = "vehicle_state";

    public string UserId { get; set; }

    public float PositionX { get; set; }
    public float PositionY { get; set; }

    public float Rotation { get; set; }

    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
}

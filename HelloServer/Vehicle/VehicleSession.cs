using System.Text.Json;

namespace HelloServer;

public class VehicleSession
{
    private readonly Func<object, Task> broadcastAsync;
    private int sceneVersion;
    
    public VehicleSession(Func<object, Task> broadcastAsync)
    {
        this.broadcastAsync = broadcastAsync;
    }

    public bool TryHandle(string type, string userId, string json)
    {
        switch (type)
        {
            case "vehicle_input":
                HandleVehicleInput(userId, json);
                return true;

            case "vehicle_state":
                HandleVehicleState(userId, json);
                return true;

            default:
                return false;
        }
    }
    
    private void HandleVehicleInput(string userId, string json)
    {
        VehicleInputMessage message =
            JsonSerializer.Deserialize<VehicleInputMessage>(json);

        if (message == null)
            return;

        message.UserId = userId;
        message.Steer = Math.Clamp(message.Steer, -1f, 1f);
        message.Accel = Math.Clamp(message.Accel, 0f, 1f);
        message.Brake = Math.Clamp(message.Brake, 0f, 1f);
        message.Gear = Math.Clamp(message.Gear, 0, 2);

        // 이 콜백은 현재 QueueBroadcast 후 CompletedTask를 반환한다.
        // Task를 기다릴 이유가 없으므로 호출만 한다.
        _ = broadcastAsync(message);
    }
    
    private void HandleVehicleState(string userId, string json)
    {
        VehicleStateMessage message =
            JsonSerializer.Deserialize<VehicleStateMessage>(json);

        if (message == null || IsValid(message) == false)
            return;

        if (message.SceneVersion != sceneVersion)
            return;

        message.UserId = userId;
        _ = broadcastAsync(message);
    }

    public void SetSceneVersion(int value)
    {
        sceneVersion = value;
    }

    private static bool IsValid(VehicleStateMessage message)
    {
        return IsFinite(message.PositionX)
               && IsFinite(message.PositionY)
               && IsFinite(message.Rotation)
               && IsFinite(message.VelocityX)
               && IsFinite(message.VelocityY);
    }

    private static bool IsFinite(float value)
    {
        return (float.IsNaN(value) == false) && (float.IsInfinity(value) == false);
    }
}
















using System.Text.Json;

namespace HelloServer;

public class VehicleSession
{
    private readonly Func<object, Task> broadcastAsync;

    public VehicleSession(Func<object, Task> broadcastAsync)
    {
        this.broadcastAsync = broadcastAsync;
    }

    public async Task<bool> TryHandleAsync(string type, string userId, string json)
    {
        switch (type)
        {
            case "vehicle_input":
                await HandleVehicleInputAsync(userId, json);
                return true;
            case "vehicle_state":
                await HandleVehicleStateAsync(userId, json);
                return true;
            default:
                return false;
        }
    }

    private async Task HandleVehicleInputAsync(string userId, string json)
    {
        VehicleInputMessage message = JsonSerializer.Deserialize<VehicleInputMessage>(json);

        if (message == null) return;

        message.UserId = userId;

        message.Steer = Math.Clamp(message.Steer, -1f, 1f);
        message.Accel = Math.Clamp(message.Accel, 0f, 1f);
        message.Brake = Math.Clamp(message.Brake, 0f, 1f);
        message.Gear = Math.Clamp(message.Gear, 0, 2);

        await broadcastAsync(message);
    }

    private async Task HandleVehicleStateAsync(string userId, string json)
    {
        VehicleStateMessage message = JsonSerializer.Deserialize<VehicleStateMessage>(json);

        if (message == null) return;
        if (IsValid(message) == false) return;

        message.UserId = userId;

        await broadcastAsync(message);
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
















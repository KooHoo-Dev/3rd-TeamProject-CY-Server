using System.Text.Json;

namespace HelloServer.MiniGames;

public class MiniGameSession
{
    private const string FuelMiniGameType = "fuel";
    private const float FuelSuccessMaxPercent = 100f;

    private static readonly TimeSpan FuelCompletionDelay = TimeSpan.FromSeconds(2);

    private enum FuelPhase
    {
        Ready,
        Fueling,
        Completed
    }
    
    private readonly Func<string[]> getMemberIds;
    private readonly Func<object, Task> broadcastAsync;
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

    private readonly HashSet<string> readyUsers = new HashSet<string>();
    
    private readonly double fuelFillSeconds;
    private readonly float fuelSuccessMinPercent;
    
    private string pendingMiniGameType;
    private string currentMiniGameType;
    
    private string fuelOperatorId;
    private FuelPhase fuelPhase;
    private DateTime fuelPressedAt;

    public MiniGameSession(Func<string[]> getMemberIds, Func<object, Task> broadcastAsync,
        double fuelFillSeconds, float fuelSuccessMinPercent)
    {
        this.getMemberIds = getMemberIds;
        this.broadcastAsync = broadcastAsync;
        this.fuelFillSeconds = fuelFillSeconds;
        this.fuelSuccessMinPercent = fuelSuccessMinPercent;
    }

    public async Task<bool> TryHandleAsync(string type, string userId, string json)
    {
        switch (type)
        {
            case "minigame_client_ready":
                await HandleClientReadyAsync(userId);
                return true;
            case "minigame_start_request":
                await HandleMiniGameStartRequestAsync(json);
                return true;
            case "fuel_press":
                await HandleFuelPressAsync(userId);
                return true;
            case "fuel_release":
                await HandleFuelReleaseAsync(userId);
                return true;
            default:
                return false;
        }
    }

    public async Task UpdateAsync()
    {
        await gate.WaitAsync();

        try
        {
            if (currentMiniGameType != FuelMiniGameType) return;
            if (fuelPhase != FuelPhase.Fueling) return;

            float guagePercent = CalculateGaugePercent();

            // 100%가 넘도록 계속 누르고 있던 경우
            if (guagePercent > FuelSuccessMaxPercent)
            {
                ResetFuelAttempt();

                await broadcastAsync(new TypeOnly { Type = "fuel_attempt_failed" });
                return;
            }

            await broadcastAsync(new FuelStateMessage
            {
                GaugePercent = guagePercent,
                IsPressed = true
            });
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task OnMemberLeftAsync(string userId)
    {
        await gate.WaitAsync();

        try
        {
            readyUsers.Remove(userId);

            if (currentMiniGameType != null)
            {
                ResetActiveMiniGame();

                await broadcastAsync(new TypeOnly
                {
                    Type = "game_aborted"
                });

                return;
            }

            await TryStartPendingMiniGameAsync();
        }
        finally
        {
            gate.Release();
        }
    }
    
    public async Task ResetForSceneChangeAsync()
    {
        await gate.WaitAsync();

        try
        {
            readyUsers.Clear();
            pendingMiniGameType = null;
            ResetActiveMiniGame();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HandleClientReadyAsync(string userId)
    {
        await gate.WaitAsync();

        try
        {
            readyUsers.Add(userId);
            await TryStartPendingMiniGameAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HandleMiniGameStartRequestAsync(string json)
    {
        MinigameStartRequestMessage request =
            JsonSerializer.Deserialize<MinigameStartRequestMessage>(json);

        string miniGameType = request?.MiniGameType?.Trim()?.ToLowerInvariant();

        if (miniGameType != FuelMiniGameType) return;

        await gate.WaitAsync();

        try
        {
            if (currentMiniGameType != null) return;
            if (pendingMiniGameType != null && pendingMiniGameType != miniGameType) return;

            pendingMiniGameType = miniGameType;

            await TryStartPendingMiniGameAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task TryStartPendingMiniGameAsync()
    {
        if (currentMiniGameType != null)
            return;

        if (pendingMiniGameType != FuelMiniGameType)
            return;

        string[] memberIds = getMemberIds();

        if (memberIds.Length == 0)
            return;

        foreach (string memberId in memberIds)
        {
            if (readyUsers.Contains(memberId) == false)
                return;
        }

        string operatorId = memberIds[Random.Shared.Next(memberIds.Length)];

        currentMiniGameType = FuelMiniGameType;
        pendingMiniGameType = null;

        fuelOperatorId = operatorId;
        fuelPhase = FuelPhase.Ready;
        fuelPressedAt = default;

        await broadcastAsync(new MinigameStartedMessage
        {
            MiniGameType = FuelMiniGameType,
            OperatorId = fuelOperatorId
        });
    }
    private async Task HandleFuelPressAsync(string userId)
    {
        await gate.WaitAsync();

        try
        {
            if (currentMiniGameType != FuelMiniGameType)
                return;

            if (fuelOperatorId != userId)
                return;

            if (fuelPhase != FuelPhase.Ready)
                return;

            fuelPhase = FuelPhase.Fueling;
            fuelPressedAt = DateTime.UtcNow;

            await broadcastAsync(new FuelStateMessage
            {
                GaugePercent = 0f,
                IsPressed = true
            });
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HandleFuelReleaseAsync(string userId)
    {
        await gate.WaitAsync();

        try
        {
            if (currentMiniGameType != FuelMiniGameType)
                return;

            if (fuelOperatorId != userId)
                return;

            if (fuelPhase != FuelPhase.Fueling)
                return;

            float gaugePercent = CalculateGaugePercent();

            if (gaugePercent >= fuelSuccessMinPercent &&
                gaugePercent <= FuelSuccessMaxPercent)
            {
                fuelPhase = FuelPhase.Completed;
                fuelPressedAt = default;

                await broadcastAsync(new TypeOnly
                {
                    Type = "fuel_completed"
                });

                _ = FinishFuelAfterDelayAsync();

                return;
            }

            ResetFuelAttempt();

            await broadcastAsync(new TypeOnly
            {
                Type = "fuel_attempt_failed"
            });
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task FinishFuelAfterDelayAsync()
    {
        await Task.Delay(FuelCompletionDelay);

        await gate.WaitAsync();

        try
        {
            if (currentMiniGameType != FuelMiniGameType)
                return;

            if (fuelPhase != FuelPhase.Completed)
                return;

            ResetActiveMiniGame();
        }
        finally
        {
            gate.Release();
        }
    }

    private float CalculateGaugePercent()
    {
        TimeSpan elapsed = DateTime.UtcNow - fuelPressedAt;

        return (float)(
            elapsed.TotalSeconds /
            fuelFillSeconds *
            100.0);
    }

    private void ResetFuelAttempt()
    {
        fuelPhase = FuelPhase.Ready;
        fuelPressedAt = default;
    }

    private void ResetActiveMiniGame()
    {
        pendingMiniGameType = null;
        currentMiniGameType = null;

        fuelOperatorId = null;
        fuelPhase = FuelPhase.Ready;
        fuelPressedAt = default;
    }
}

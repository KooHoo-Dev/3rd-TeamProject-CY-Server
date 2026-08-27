using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace HelloServer;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.Services.ConfigureHttpJsonOptions(
            options => options.SerializerOptions.PropertyNamingPolicy = null);
        
        WebApplication app = builder.Build();
        
        // 앱의 구성에 값을 가져온다 "Room:BroadcastPerSecond" 키의 값을, 없다면 10을 넣는다
        int perSecond = app.Configuration.GetValue("Room:BroadcastPerSecond", 10);
        
        // 앱의 구성에 값을 가져온다 "Room:LogMovesPerSecond" 키의 값을, 없다면 1을 넣는다
        int logMoves = app.Configuration.GetValue("Room:LogMovesPerSecond", 1);
        
        // 서버에 방을 추가해 줍시다.
        RoomHub hub = new RoomHub(perSecond, logMoves);
        
        app.UseWebSockets();
        app.MapGet("/ping", () => "pong");

        // 방으로 들어오는 문
        // 여기서 await하는 동안 그 사람의 연결이 살아있습니다.
        // 여기서 hub.HandleAsync => room.HandleAsync를 호출하여
        // 한 유저의 접속부터 끊김까지 바인딩해줍니다.
        app.Map("/room", async context =>
        {
            // 웹소켓으로 접속했니?
            if (context.WebSockets.IsWebSocketRequest == false)
            {
                // 아니라면 평범한 브라우저의 접속임
                // StatusCodes = 404 notfound 뭐 그런거 모였있는겁니다.
                // 표준적인 에러처리들
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("웹소켓으로 접속하시오");
                return;
            }

            // 방코드는 쿼리 스트링을 통해서 주소에 실려오도록 설계되었습니다
            // ex) ws://localhost:5000/room?code=ABCE
            
            string code = RoomHub.Normalize(context.Request.Query["code"]);
            if (string.IsNullOrEmpty(code))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("방코드 해석 불가능");
                return;
            }
            
            // 여기까지 오면 예외처리 완료된것
            // 소켓을 만들어 준다(연결을 받아준다)
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await hub.HandleAsync(code, socket, context.RequestAborted);
        });

        // 어플리케이션이 종료될때까지 허브가 Broadcast 루프를 돌도록 설정해준다.
        // _ : 반환형이 있지만 안쓸때 언더바 사용함
        _ = hub.BroadcastLoopAsync(app.Lifetime.ApplicationStopped);
        
        app.Run();
    }
}

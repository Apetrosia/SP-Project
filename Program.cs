using FrogChess.Server.Hubs;
using FrogChess.Server.Services;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);  
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2); 
    options.MaximumReceiveMessageSize = 32 * 1024;   
});

builder.Services.AddSingleton<GameManager>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true); 
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseDefaultFiles();  
app.UseStaticFiles(); 

app.UseCors();
app.MapHub<GameHub>("/gamehub");

app.Run();
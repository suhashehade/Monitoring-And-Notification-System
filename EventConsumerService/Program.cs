using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = "http://localhost:5085/alertsHub";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect() 
    .Build();


connection.On<object>("AnomalyAlert", (alert) =>
{
    Console.WriteLine($"[ANOMALY ALERT] {alert}");
});

connection.On<object>("HighUsageAlert", (alert) =>
{
    Console.WriteLine($"[HIGH USAGE ALERT] {alert}");
});

try
{
    await connection.StartAsync();
    Console.WriteLine("Connected to AlertsHub. Listening for alerts...");
    Console.WriteLine("Press any key to exit.\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect: {ex.Message}");
}

Console.ReadKey();
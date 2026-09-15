using Messaging;
using Messaging.RabbitMQ;
using Stat_Collector_Service.StatCollectProviders;
using Stat_Collector_Service.StatCollectProviders.Interfaces;

namespace Stat_Collector_Service;

public static class Program { 
   public static void Main(string[] args)
   {
      var builder = Host.CreateApplicationBuilder(args);
      builder.Services.AddSingleton<ISystemStatsProvider, WindowsStatsProvider>();
      builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
      builder.Services.AddHostedService<Worker>();
      var host = builder.Build();
      host.Run();
   }
}
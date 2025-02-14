﻿// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Tools.Singleton;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Routing;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebCrawler.Shared.DevOps;
using WebCrawler.Web.Actors;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var webBuilder = WebApplication.CreateBuilder();

            webBuilder.Configuration.AddEnvironmentVariables();
            webBuilder.WebHost
                .UseKestrel()
                .ConfigureServices((context, services) =>
                {
                    services.AddControllersWithViews();
                    services.AddSignalR();
                    services.AddSingleton<CrawlHubHelper, CrawlHubHelper>();

                    // Add Akka hosted service
                    services.AddAkka("webcrawler", (builder, provider) =>
                    {
                        builder
                            .AddHoconFile(hoconFilePath: "web.hocon", addMode: HoconAddMode.Prepend)
                            .WithRemoting(hostname: "localhost", port: 16666)
                            // Add common DevOps settings
                            .WithOps(
                                remoteOptions: new RemoteOptions
                                {
                                    HostName = "0.0.0.0",
                                    Port = 16666
                                },
                                clusterOptions: new ClusterOptions
                                {
                                    SeedNodes = new[] { "akka.tcp://webcrawler@localhost:16666" },
                                    Roles = new[] { "web" },
                                }, 
                                config: context.Configuration)
                            // Instantiate actors
                            .WithActors((system, registry) =>
                            {
                                var router = system.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "tasker");
                                var processor = system.ActorOf(
                                    Props.Create(() => new CommandProcessor(router)),
                                    "commands");

                                var singletonProxy = system.ActorOf(ClusterSingletonProxy.Props(
                                    singletonManagerPath: "/user/customsingleton",
                                    settings: ClusterSingletonProxySettings.Create(system)
                                    .WithRole("tracker")),
                                    name: "singletonProxy");

                                var webActor = system.ActorOf(
                                    Props.Create(() => new WebActor(singletonProxy)),
                                    "webactor");

                                var signalRProps = DependencyResolver.For(system).Props<SignalRActor>(processor);
                                var signalRActor = system.ActorOf(signalRProps, "signalr");
                                registry.Register<SignalRActor>(signalRActor);
                            });
                    });
                });

            var app = webBuilder.Build();

            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/error");

            app.UseStaticFiles()
                .UseRouting()
                .UseEndpoints(ep =>
                {
                    ep.MapControllerRoute("default",
                        "{controller=Home}/{action=Index}/{id?}");
                    ep.MapHub<CrawlHub>("/hubs/crawlHub");
                });

            await app.RunAsync();
        }
    }
}
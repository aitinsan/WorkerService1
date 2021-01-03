using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WorkerService1
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher watcher;
        private readonly IConfiguration configuration;
        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            this.configuration = configuration;
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = configuration["PathFolder"];
            watcher.Created += OnCreated;
            watcher.Changed += OnChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;

            _logger.LogInformation("-----Folder Monitoring Started-------");

            return base.StartAsync(cancellationToken);
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            _logger.LogInformation("RENAMED: A new message is about to be sent at : {time}", DateTimeOffset.Now);
            SendMessage(e.FullPath, e.ChangeType.ToString());
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("DELETED: A new message is about to be sent at : {time}", DateTimeOffset.Now);
            SendMessage(e.FullPath, e.ChangeType.ToString());
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("CHANGED: A new message is about to be sent at : {time}", DateTimeOffset.Now);
            SendMessage(e.FullPath, e.ChangeType.ToString());
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("CREATED: A new message is about to be sent at : {time}", DateTimeOffset.Now);
            SendMessage(e.FullPath, e.ChangeType.ToString());
        }
        private async Task SendMessage(string fullPath, string changes)
        {
            var message = new
            {
                Type = "email",
                JsonContent = "Hello from WSFolderWatcher. A file " + fullPath + " was " + changes,
            };

            var json = JsonConvert.SerializeObject(message);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("http://localhost:59138/api/queue/add", data);
                string result = response.Content.ReadAsStringAsync().Result;
                _logger.LogInformation(result);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!Directory.Exists(configuration["PathFolder"]))
                {
                    Directory.CreateDirectory(configuration["PathFolder"]);
                }
                watcher.EnableRaisingEvents = true;

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("-----------Folder Monitoring Service Stopped------");
            return base.StopAsync(cancellationToken);
        }
    }
}

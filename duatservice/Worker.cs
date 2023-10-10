using Grpc.Core;
using optimus.duat.lib.task;
using optimus.duat.service;
using System.Text.Json;

namespace duatservice;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private DocumentTask? documentTask;

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation($"file {e.FullPath} created");
        documentTask?.UpdateFiles(e.FullPath);
    }
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogInformation($"file {e.FullPath} changed");
        documentTask?.UpdateFiles(e.FullPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pwd = Directory.GetCurrentDirectory();
        var filename = @$"{appData}\config.json";
        if (!File.Exists("config.json"))
        {
            if (!File.Exists(filename))
            {
                logger.LogError($"File config.json not found in {appData} or {pwd}.");
                return;
            }
        }
        else filename = "config.json";

        DuatConfig? config;

        try
        {
            var data = File.ReadAllText(filename);
            config = JsonSerializer.Deserialize<DuatConfig>(data);
        }
        catch (Exception ex)
        {
            logger.LogError($"Load configuration error [{ex.Message}]");
            return;
        }
        if (config == null)
        {
            return;
        }
        if (config.Hostname == null || config.Hostname.Length == 0)
        {
            logger.LogError($"Get host from configuration error, key not found or invalid value");
            return;
        }
        if (config.Port == null)
        {
            logger.LogError($"Get port from configuration error, key not found or invalid value");
            return;
        }
        if (config.Token == null || config.Token.Length == 0)
        {
            if (config.Username == null || config.Username.Length == 0)
            {
                logger.LogError($"Get username from configuration error, key not found or invalid value");
                return;
            }
            if (config.Password == null || config.Password.Length == 0)
            {
                logger.LogError($"Get password from configuration error, key not found or invalid value");
                return;
            }
        }
        if (config.Path == null || config.Hostname.Length == 0)
        {
            logger.LogError($"Get path from configuration error, key not found or invalid value");
            return;
        }
        if (!Directory.Exists(config.Path))
        {
            logger.LogError($"Watch path {config.Path} not exists");
            return;
        }

        var channel = new Channel($"{config.Hostname}:{config.Port}", ChannelCredentials.Insecure);
        documentTask = new DocumentTask(channel);

        var watcher = new FileSystemWatcher(config.Path)
        {
            Filter = "data.json",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;


        var dirs = Directory.GetDirectories(config.Path);

        foreach (var dir in dirs)
        {
            var dataFile = @$"{dir}\data.json";
            if (!File.Exists(filename)) continue;
            documentTask.UpdateFiles(dataFile);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}

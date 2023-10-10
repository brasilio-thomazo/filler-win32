using Grpc.Core;
using System.Text.Json;
using optimus.duat.lib.model;
using static Duat.DocumentService;
using static Duat.DocumentImageService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace optimus.duat.lib.task
{
    public class DocumentTask
    {
        private readonly ILogger<DocumentTask> logger;
        public static readonly List<string> Files = new();
        private readonly DocumentServiceClient clientDocument;
        private readonly DocumentImageServiceClient clientDocumentImage;
        private static int ParallelTasks { get; set; } = 5;
        public readonly static List<Task> Tasks = new();
        public readonly static List<Thread> Threads = new();
        public static int TaskRunning = 0;


        public DocumentTask(Channel channel)
        {
            clientDocument = new DocumentServiceClient(channel);
            clientDocumentImage = new DocumentImageServiceClient(channel);
            using IHost host = Host.CreateDefaultBuilder().Build();
            logger = host.Services.GetRequiredService<ILogger<DocumentTask>>();
            logger.LogInformation("DocumentTask::New");
        }

        private static bool InUse(string filename)
        {
            try
            {
                using var fs = File.OpenWrite(filename);
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }

        public static bool DocumentCompleted(Document document)
        {
            if (document.Id == null) return false;
            if (document.Id.Length == 0) return false;
            if (document.Error.StatusCode != StatusCode.OK && document.Error.StatusCode != StatusCode.Unavailable) return true;
            var imgCompleted = 0;
            foreach (var img in document.Images)
            {
                if (img.Error.StatusCode != StatusCode.OK && img.Error.StatusCode != StatusCode.Unavailable) imgCompleted++;
                else if (img.CompletedAt > 0) imgCompleted++;
            }
            return imgCompleted >= document.Images.Length;
        }

        private Document? GetDocument(FileInfo info)
        {
            while (InUse(info.FullName)) Thread.Sleep(1000);
            try
            {
                var data = File.ReadAllText(info.FullName);
                var document = JsonSerializer.Deserialize<Document>(data);
                if (document == null) { return null; }
                if (!document.IsDone) { return null; }
                if (document.Uploaded >= document.Images.Length) { return null; }
                if (document.Error != null && document.Error.StatusCode != StatusCode.OK && document.Error.StatusCode != StatusCode.Unavailable) return null;
                return document;
            }
            catch (Exception ex)
            {
                logger.LogError("DocumentTask::GetDocument {}", ex.Message);
            }
            return null;
        }

        public static void StartNext(FileInfo info, Document document)
        {
            if (TaskRunning > 0) TaskRunning--;
            UpdateFile(info, document);
            if (Tasks.Count == 0) return;
            int start = TaskRunning;
            int end = Math.Min(ParallelTasks, Tasks.Count);
            for (var i = start; i < end; i++)
            {
                Tasks.First().Start();
                Tasks.RemoveAt(0);
                TaskRunning += 1;
            }
        }

        public static void UpdateFile(FileInfo info, Document document)
        {
            if (!DocumentCompleted(document)) return;
            while (InUse(info.FullName)) Thread.Sleep(1000);
            var data = JsonSerializer.Serialize(document);
            File.WriteAllText(info.FullName, data);
            if (Files.Contains(info.FullName)) Files.Remove(info.FullName);
        }

        public void UpdateFiles(string filename)
        {
            logger.LogInformation("DocumentTask::UpdateFiles({})", filename);
            if (Files.Contains(filename) || !File.Exists(filename))
            {
                logger.LogInformation("DocumentTask::UpdateFiles({}) [PASS]", filename);
                return;
            }
            var info = new FileInfo(filename);
            var document = GetDocument(info);
            if (document == null) { return; }
            if (DocumentCompleted(document)) { return; }

            Files.Add(filename);
            var documetStore = new DocumentStoreTask(info, document, clientDocument, clientDocumentImage);
            Tasks.Add(new Task(documetStore.Store));
            StartNext(info, document);
        }

    }

}

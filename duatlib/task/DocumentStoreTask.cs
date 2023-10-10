using Duat;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using optimus.duat.lib.model;
using static Duat.DocumentImageService;
using static Duat.DocumentService;

namespace optimus.duat.lib.task
{
    public class DocumentStoreTask
    {
        private readonly ILogger<DocumentStoreTask> logger;
        private readonly FileInfo info;
        private readonly Document document;
        private readonly DocumentServiceClient client;
        private readonly DocumentImageServiceClient imgClient;

        public DocumentStoreTask(FileInfo info, Document document, DocumentServiceClient client, DocumentImageServiceClient imageClient)
        {
            this.info = info;
            this.document = document;
            this.client = client;
            imgClient = imageClient;
            using IHost host = Host.CreateDefaultBuilder().Build();
            logger = host.Services.GetRequiredService<ILogger<DocumentStoreTask>>();
            logger.LogInformation("DocumentStoreTask::New");
        }

        private void CreateImageTasks()
        {
            for (uint i = 0; i < document.Images.Length; i++)
            {
                if (document.Id == string.Empty) { continue; }
                if (document.Images[i] == null) { continue; }
                if (document.Images[i].Error != null && document.Images[i].Error.StatusCode != StatusCode.OK && document.Images[i].Error.StatusCode != StatusCode.Unavailable) continue;
                if (document.Images[i].CompletedAt > 0) { continue; }
                if (document.Images[i].Path == null || document.Images[i].Path == string.Empty)
                {
                    if (info.DirectoryName == null) { continue; }
                    document.Images[i].Path = info.DirectoryName;
                }
                if (!Directory.Exists(document.Images[i].Path)) { continue; }
                document.Images[i].Filename = @$"{info.DirectoryName}\{document.Images[i].Filename}";

                if (!File.Exists(document.Images[i].Filename)) { continue; }
                document.Images[i].DocumentId = document.Id;

                var imageStore = new DocumentImageStoreTask(info, document, i, imgClient);
                DocumentTask.Tasks.Add(new Task(imageStore.Store));
            }
        }

        public async void Store()
        {
            if (document.Error.StatusCode == StatusCode.Unavailable) Thread.Sleep(5000);

            var request = new DocumentRequest()
            {
                DocumentTypeId = document.DocumentTypeId,
                DepartmentId = document.DepartmentId,
                Code = document.Code,
                Identity = document.Identity,
                Name = document.Name,
                DateDocument = document.DateDocument,
            };

            if (document.Comment != string.Empty) request.Comment = document.Comment;
            if (document.Storage != string.Empty) request.Storage = document.Storage;

            try
            {
                var reply = await client.StoreAsync(request);
                document.Id = reply.Id;
                CreateImageTasks();
            }
            catch (RpcException ex)
            {
                LogError(ex);
                document.Error = new ErrorReply()
                {
                    StatusCode = ex.StatusCode,
                    Detail = ex.Status.Detail,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                document.Error = new ErrorReply()
                {
                    Message = ex.Message,
                    StatusCode = StatusCode.Internal
                };
            }
            finally
            {
                DocumentTask.StartNext(info, document);
            }
        }

        private void LogError(Exception exception)
        {
            try
            {
                logger.LogError(exception.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }

}
using Duat;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using optimus.duat.lib.model;
using static Duat.DocumentImageService;

namespace optimus.duat.lib.task
{
    public class DocumentImageStoreTask
    {
        private readonly ILogger<DocumentImageStoreTask> logger;
        private readonly FileInfo info;
        private readonly Document document;
        private readonly uint index;
        private readonly DocumentImageServiceClient client;



        public DocumentImageStoreTask(FileInfo info, Document document, uint index, DocumentImageServiceClient client)
        {
            this.info = info;
            this.document = document;
            this.index = index;
            this.client = client;
            using IHost host = Host.CreateDefaultBuilder().Build();
            logger = host.Services.GetRequiredService<ILogger<DocumentImageStoreTask>>();
            logger.LogInformation("DocumentImageStoreTask::New");
        }

        public async void Store()
        {
            if (document.Images.Length == 0 || index >= document.Images.Length)
            {
                DocumentTask.StartNext(info, document);
                return;
            }
            if (document.Images[index] == null)
            {
                DocumentTask.StartNext(info, document);
                return;
            }

            if (document.Images[index].Error.StatusCode == StatusCode.Unavailable) Thread.Sleep(5000);

            document.Images[index].StartedAt = DateTimeOffset.Now.ToUnixTimeSeconds();
            document.Images[index].Running = true;
            document.Images[index].DocumentId = document.Id;

            try
            {
                using var send = File.OpenRead(document.Images[index].Filename);
                var stream = client.Store();
                var finfo = new FileInfo(document.Images[index].Filename);
                byte[] buffer = new byte[4096];
                int bytesRead;

                var data = new DocumentImageRequest()
                {
                    DocumentId = document.Id,
                    ImageExt = finfo.Extension,
                    Page = index + 1,
                    StorageType = StorageType.Local
                };

                while ((bytesRead = send.Read(buffer, 0, buffer.Length)) > 0)
                {
                    data.Data = ByteString.CopyFrom(buffer, 0, bytesRead);
                    await stream.RequestStream.WriteAsync(data);
                }

                await stream.RequestStream.CompleteAsync();
                var reply = await stream.ResponseAsync;
                document.Images[index].CompletedAt = DateTimeOffset.Now.ToUnixTimeSeconds();
                document.Uploaded += 1;
            }
            catch (RpcException ex)
            {
                document.Images[index].Error = new ErrorReply()
                {
                    StatusCode = ex.StatusCode,
                    Detail = ex.Status.Detail,
                    Message = ex.Message
                };
                LogError(ex);
            }
            catch (Exception ex)
            {
                document.Images[index].Error = new ErrorReply()
                {
                    Message = ex.Message,
                    StatusCode = StatusCode.Internal
                };
                LogError(ex);
            }
            finally
            {
                document.Images[index].Running = false;
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
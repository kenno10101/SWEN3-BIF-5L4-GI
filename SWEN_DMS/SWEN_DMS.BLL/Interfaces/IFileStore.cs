namespace SWEN_DMS.BLL.Interfaces;

public interface IFileStore
{
    Task SaveAsync(string bucket, string objectKey, Stream content, string contentType, long size, CancellationToken ct = default);
}
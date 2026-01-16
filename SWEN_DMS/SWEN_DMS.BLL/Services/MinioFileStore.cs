using Minio;
using Minio.DataModel.Args;
using SWEN_DMS.BLL.Interfaces;

namespace SWEN_DMS.BLL.Services;

public class MinioFileStore : IFileStore
{
    private readonly IMinioClient _minio;

    public MinioFileStore(IMinioClient minio)
    {
        _minio = minio;
    }

    public async Task SaveAsync(string bucket, string objectKey, Stream content, string contentType, long size, CancellationToken ct = default)
    {
        await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithStreamData(content)
                .WithObjectSize(size)
                .WithContentType(contentType),
            ct
        );
    }
}
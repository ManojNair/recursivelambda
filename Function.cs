using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Serialization.Json;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;


[assembly: LambdaSerializer(typeof(JsonSerializer))]

namespace LetsCode
{
    public class Function
    {
        private readonly IAmazonS3 _client = new AmazonS3Client(RegionEndpoint.APSoutheast2);

        public async Task<string> FunctionHandler(S3Event input, ILambdaContext context)
        {
            var extensions = new[] {".jpg", ".jpeg"};
            var s3Event = input.Records?[0].S3;
            var bucket = s3Event?.Bucket.Name;
            var key = s3Event?.Object.Key;
            var keyData = await _client.GetObjectAsync(bucket, key, CancellationToken.None);


            context.Logger.LogLine(Path.GetTempPath());
            if (!extensions.Contains(Path.GetExtension(key).ToLower())) return "Done";
            using (var image = Image.Load(keyData.ResponseStream))
            {
                image.Mutate(processingContext => processingContext.Resize(50, 50));

                using (var ms = new MemoryStream())
                {
                    image.Save(ms,new JpegEncoder(){Quality = 100});

                    await _client.PutObjectAsync(new PutObjectRequest()
                    {
                        BucketName = bucket,
                        Key = $"{key?.Split('.')[0]}-small.{key?.Split('.')[1]}",
                        InputStream = ms
                    });
                }
            }

            return "Done";
        }
    }
}
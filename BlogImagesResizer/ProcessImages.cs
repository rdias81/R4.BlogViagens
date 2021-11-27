using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BlogImagesResizer
{
    [StorageAccount("BlobStorageConnection")]
    public static class ProcessImages
    {
        [FunctionName("ProcessImages")]
        public static void Run([BlobTrigger("imagens/{name}")] Stream image,
            [Blob("thumbs/{name}", FileAccess.Write)] Stream thumbContainer,
            [Blob("imagens-resize/{name}", FileAccess.Write)] Stream optimizedContainer,
            ILogger log)
        {
            IImageFormat format;
            
            log.LogInformation("Iniciando redimensionamento de imagem.");

            try
            {
                using (Image<Rgba32> input = Image.Load<Rgba32>(image, out format))
                {
                    ResizeImage(input, thumbContainer, ImageSize.Thumb, format);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }

            image.Position = 0;

            try
            {
                using (Image<Rgba32> input = Image.Load<Rgba32>(image, out format))
                {
                    ResizeImage(input, optimizedContainer, ImageSize.Optimized, format);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
            
            log.LogInformation("Redimensionamento concluído.");
        }

        private static void ResizeImage(Image<Rgba32> input, Stream output, ImageSize size, IImageFormat format)
        {
            var dimensions = imageDimensionsTable[size];

            input.Mutate(x => x.Resize(dimensions.Item1, dimensions.Item2));
            input.Save(output, format);
        }

        private enum ImageSize { Thumb, Optimized }

        private static Dictionary<ImageSize, (int, int)> imageDimensionsTable = new Dictionary<ImageSize, (int, int)>() {
            { ImageSize.Thumb,      (320, 200) },
            { ImageSize.Optimized,  (640, 400) }
        };
    }
}

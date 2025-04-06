
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SkiaSharp;
namespace Ertaqy.Storage;


public class ImageResizeMiddleware : IMiddleware
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public ImageResizeMiddleware(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (IsImageRequest(context.Request))
        {
            await ResizeImageAsync(context, next);
        }
        else
        {
            await next(context);
        }
    }

    private bool IsImageRequest(HttpRequest request)
    {
        // Check if the request path indicates an image request
        var path = request.Path;
        return path.HasValue && path.Value.Contains("/images/");
    }

    private async Task<byte[]> DownloadImageAsync(string imageUrl)
    {

        byte[] imageData = await File.ReadAllBytesAsync(imageUrl);
        return imageData;

    }
    private async Task ResizeImageAsync(HttpContext context, RequestDelegate next)
    {
        // Extract image name and size from URL
        var pathSegments = context.Request.Path.Value.Split('/');
        var imageName = pathSegments[pathSegments.Length - 1].Split('?')[0];
        var queryString = context.Request.QueryString.Value;
        if (!string.IsNullOrEmpty(queryString))
        {
            int size = 0;
            var parts = queryString.Split('?');
            //if (context.Request.Query.TryGetValue("size", out var sizeValue) && int.TryParse(sizeValue, out var size))
            if (parts.Length == 2 && int.TryParse(parts[1], out size))

            {
                // Construct image URL
                var imageUrl = _hostingEnvironment.WebRootPath + "" + context.Request.Path; //GetImageUrl(context, imageName);
                Console.WriteLine(imageUrl);

                if (!File.Exists(imageUrl))
                {
                    return;
                }
                // Replace "images" with "image_thumb" in the path
                var resizedImagePath = $"{_hostingEnvironment.WebRootPath}{context.Request.Path}".Replace("/images/", "/images-thumbs/");
                resizedImagePath = $"{Path.GetDirectoryName(resizedImagePath)}/{Path.GetFileNameWithoutExtension(imageName)}_{size}{Path.GetExtension(imageName)}";

                // Create the "image_thumb" folder if it doesn't exist
                var directory = Path.GetDirectoryName(resizedImagePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }




                if (File.Exists(resizedImagePath))
                {
                    // If the resized image exists, return it without resizing
                    var existingImageData = await File.ReadAllBytesAsync(resizedImagePath);
                    context.Response.ContentType = "image/jpeg";
                    await context.Response.Body.WriteAsync(existingImageData, 0, existingImageData.Length);
                }





                else
                {

                    // Download image
                    var resizedImageData = ResizeWithSkiaSharp(imageUrl, size);
                    // Save resized image to the new path
                    await File.WriteAllBytesAsync(resizedImagePath, resizedImageData);
                    // Set response content type
                    context.Response.ContentType = "image/jpeg";

                    // Write resized image data to response
                    await context.Response.Body.WriteAsync(resizedImageData, 0, resizedImageData.Length);
                }
            }
            else
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }

    private byte[] ResizeImage(byte[] imageData, int width, int height)
    {
        using (var inputStream = new MemoryStream(imageData))
        using (var outputStream = new MemoryStream())
        {
            // Load image from stream
            using (var original = SKBitmap.Decode(inputStream))
            {
                // Resize image
                using (var resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High))
                {
                    // Encode resized image to JPEG format
                    resized.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(outputStream);
                }
            }

            return outputStream.ToArray();
        }
    }

    private string GetImageUrl(HttpContext context, string imageName)
    {
        // Construct image URL based on the incoming request's host and scheme
        var imageUrl = $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}";
        return imageUrl;
    }

    public byte[] ResizeWithSkiaSharp(string url, int Width)
    {
        using var stream = GetStream(url);
        using var skData = SKData.Create(stream);
        using var codec = SKCodec.Create(skData);

        float aspectRatio = (float)codec.Info.Height / codec.Info.Width;
        int Height = (int)(Width * aspectRatio);
        var supportedScale = codec
             .GetScaledDimensions((float)Width / codec.Info.Width);

        var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
        using var destinationImage = SKBitmap.Decode(codec, nearest);
        using var resizedImage = destinationImage.Resize(new SKImageInfo(Width, Height), SKFilterQuality.High);


        var format = SKEncodedImageFormat.Png;
        using var outputImage = SKImage.FromBitmap(resizedImage);
        using var data = outputImage.Encode(format, 90);
        //using var outputStream = GetOutputStream("skiasharp");
        //data.SaveTo(outputStream);

        //outputStream.Close();
        //stream.Close();
        return data.ToArray();
    }
    public byte[] ResizeWithSkiaSharp2(string url, int width)
    {
        using var stream = GetStream(url);
        using var skData = SKData.Create(stream);
        using var codec = SKCodec.Create(skData);

        float aspectRatio = (float)codec.Info.Height / codec.Info.Width;
        int height = (int)(width * aspectRatio);

        var nearest = new SKImageInfo(width, height);
        using var destinationImage = SKBitmap.Decode(codec, nearest);
        using var resizedImage = destinationImage.Resize(new SKImageInfo(width, height), SKFilterQuality.High);

        var format = SKEncodedImageFormat.Png;
        using var outputImage = SKImage.FromBitmap(resizedImage);
        using var data = outputImage.Encode(format, 90);

        return data.ToArray();
    }
    private static Stream GetOutputStream(string name)
    {
        return File.Open($"images/output_{name}.png", FileMode.OpenOrCreate);
    }

    private static Stream GetStream(string url)
    {
        return File.OpenRead(url);//"images/input.jpeg";
    }
}
public static class ImageResizeMiddlewareExtensions
{
    public static IApplicationBuilder UseImageResizeMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ImageResizeMiddleware>();
    }
}

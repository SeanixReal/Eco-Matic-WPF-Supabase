using System;
using System.Windows.Media.Imaging;

namespace Eco_Matic;

public static class ImageLoader
{
    public static BitmapImage? LoadProductImage(string relativeImagePath)
    {
        if (string.IsNullOrWhiteSpace(relativeImagePath))
            return null;
        
        string cleanPath = relativeImagePath.TrimStart('/', '\\').Replace('\\', '/');
        string packUri = $"pack://application:,,,/{cleanPath}";

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(packUri, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return LoadFromPath(relativeImagePath); // fallback
        }
    }

    public static BitmapImage? LoadFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, filePath.TrimStart('/', '\\')));
            if (!System.IO.File.Exists(fullPath)) return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(fullPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}

using System;
using System.Windows.Media.Imaging;
using System.IO;

namespace Eco_Matic;

public static class ImageLoader
{
    public static BitmapImage? LoadProductImage(string relativeImagePath)
    {
        const string defaultPlaceholder = "Assets/Images/placeholder.png";

        // 1. Try specified path
        if (!string.IsNullOrWhiteSpace(relativeImagePath))
        {
            var img = TryLoad(relativeImagePath);
            if (img != null) return img;

            // 1.1 Try prepending folder if missing
            if (!relativeImagePath.Contains('/') && !relativeImagePath.Contains('\\'))
            {
                var fallbackImg = TryLoad("Assets/Images/" + relativeImagePath);
                if (fallbackImg != null) return fallbackImg;
            }
        }

        // 2. Try default placeholder
        return TryLoad(defaultPlaceholder);
    }

    private static BitmapImage? TryLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // Normalize path for URI consistency
        string normalizedPath = path.Replace('\\', '/').TrimStart('/');
        if (!normalizedPath.StartsWith("Assets/Images/", StringComparison.OrdinalIgnoreCase) && 
            !normalizedPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = "Assets/Images/" + normalizedPath;
        }

        // We try with the project namespace first
        string[] resourcePaths = {
            $"pack://application:,,,/Eco_Matic;component/{normalizedPath}",
            $"pack://application:,,,/Eco-Matic;component/{normalizedPath}",
            $"pack://application:,,,/{normalizedPath}"
        };

        foreach (var uriStr in resourcePaths)
        {
            try
            {
                var uri = new Uri(uriStr, UriKind.Absolute);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = uri;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch { }
        }

        // 2. Try as Local File
        try
        {
            string osPath = normalizedPath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(AppContext.BaseDirectory, osPath);

            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? "", osPath);
            }

            if (File.Exists(fullPath))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(fullPath);
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
        catch { }

        return null;
    }
}

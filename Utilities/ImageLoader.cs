using System.IO;
using System.Windows.Media.Imaging;

namespace Eco_Matic;

public static class ImageLoader
{
    public static BitmapImage? LoadProductImage(string relativeImagePath)
    {
        if (string.IsNullOrWhiteSpace(relativeImagePath))
        {
            return null;
        }

        return LoadFromPath(CsvStorage.GetImageFullPath(relativeImagePath));
    }

    public static BitmapImage? LoadFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath, UriKind.Absolute);
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

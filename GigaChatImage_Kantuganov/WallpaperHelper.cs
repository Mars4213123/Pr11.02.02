using System.IO;
using System.Runtime.InteropServices;

namespace GigaChatImage_Kantuganov
{
    public static class WallpaperHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        public static bool SetWallpaper(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                    return result != 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
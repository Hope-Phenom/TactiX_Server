using NuGet.Versioning;

namespace TactiX_Server
{
    public static class Tools
    {
        /// <summary>
        /// 比较两个版本号，使用NuGet.Versioning实现
        /// </summary>
        /// <param name="ver1">版本号1</param>
        /// <param name="ver2">版本号2</param>
        /// <returns>0-相等，1-ver2大于ver1，-1-ver2小于ver1</returns>
        public static int CompareVersion(string ver1, string ver2)
        {
            NuGetVersion v1 = NuGetVersion.Parse(ver1);
            NuGetVersion v2 = NuGetVersion.Parse(ver2);

            return v1.CompareTo(v2);
        }
    }
}

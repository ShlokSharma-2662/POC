using System;
using System.IO;

namespace Ecommerce.Tests.Common
{
    public static class TestPathHelper
    {
        public static string CreateWritableContentRootPath(string subfolder = "ecommerce-tests")
        {
            var rootPath = Path.Combine(Path.GetTempPath(), subfolder, Guid.NewGuid().ToString("N"));
            var uploadsPath = Path.Combine(rootPath, "Uploads", "products");
            Directory.CreateDirectory(uploadsPath);
            return rootPath;
        }
    }
}

using HlidacStatu.Util;
using System;

namespace HlidacStatu.Repositories
{
    public class SharedFiles
    {
        private static volatile Devmasters.Cache.AWS_S3.Manager<byte[], string> sharedFileManager =
            Devmasters.Cache.AWS_S3.Manager<byte[], string>.GetSafeInstance(
                    "sharedFiles/",
                    key => Array.Empty<byte>(),
                    TimeSpan.Zero,
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                    key => $"sharedFiles/{key}.bin"
                    );
        private static volatile Devmasters.Cache.AWS_S3.Manager<Info, string> sharedFileManagerInfo =
            Devmasters.Cache.AWS_S3.Manager<Info, string>.GetSafeInstance(
                    "sharedFiles/",
                    key => null,
                    TimeSpan.Zero,
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                    key => $"sharedFiles/{key}.info"
                    );

        public static volatile SharedFiles Manager = new SharedFiles();

        private SharedFiles() { }

        public Info Get(string filename)
        {
            string fn = Devmasters.Crypto.Hash.ComputeHashToHex(filename);
            if (sharedFileManager.Exists(fn))
            {
                var info = sharedFileManagerInfo.Get(fn);
                info.Data = sharedFileManager.Get(fn);
                return info;
        }
            else
                return null;
        }

        public bool Exists(string filename)
        {
            string fn = Devmasters.Crypto.Hash.ComputeHashToHex(filename);
            return sharedFileManager.Exists(fn);
        }

        public void Delete(string filename)
        {
            string fn = Devmasters.Crypto.Hash.ComputeHashToHex(filename);
            sharedFileManager.Delete(fn);
        }

        public void Put(string filename, byte[] data, string contentType = null)
        {
            Info i = new Info();
            i.Filename = filename;
            if (contentType != null)
                i.ContentType = contentType;

            string fn = Devmasters.Crypto.Hash.ComputeHashToHex(filename);

            sharedFileManager.Set(fn, data);
            sharedFileManagerInfo.Set(fn, i);
        }
        public string Put(byte[] data, string contentType = null)
        {
            string fn = Devmasters.Crypto.Hash.ComputeHashToHex(data);
            Put(fn, data, contentType);
            return fn;
        }


        public Uri GetAdminUrl(string filename)
        {
            return new Uri($"https://ladmin.hlidacstatu.cz/mvc/home/getfile?id={filename}");
        }

        public class Info
        {
            public DateTime Created { get; set; } = DateTime.UtcNow;
            public string Filename { get; set; }
            public string ContentType { get; set; } = "application/octet-stream";

            [Newtonsoft.Json.JsonIgnore]
            [System.Text.Json.Serialization.JsonIgnore]
            public byte[] Data { get; set; }
        }
    }
}

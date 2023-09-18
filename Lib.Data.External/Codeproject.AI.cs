using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Codeproject.AI
{
    public class ApiResponseException : ApplicationException
    {
        public ApiResponseException()
        {
        }

        public ApiResponseException(string message) : base(message)
        {
        }

        public ApiResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ApiResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public partial class Client
    {
        static string DefaultApiServerRoot = "http://10.10.100.113:32168";

        public Uri ApiServerRoot { get; }

        public Client() 
            : this(new Uri(DefaultApiServerRoot))
        {}
        public Client(Uri apiServerRoot)
        {
            ApiServerRoot = apiServerRoot;
        }

        public  async Task<byte[]> ImagePortraitFilterAsync(byte[] bytesContent, float strength = 0.5f)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytesContent), "image", "anyfile.jpg");
            form.Add(new StringContent(strength.ToString(HlidacStatu.Util.Consts.enCulture).ToLower()), "strength");
            var res = await Devmasters.Net.HttpClient.Simple.PostAsync<PortraitFilterResponse>(this.ApiServerRoot + "v1/image/portraitfilter",
                form);

            if (res?.success == true)
            {
                return res.imageData();

            }
            else
                throw new ApiResponseException(res.error);
        }

        public  async Task<byte[]> ImageRemoveBackgroundAsync(byte[] bytesContent, bool use_alphamatting = false)
        {
            try
            {

            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytesContent), "image", "anyfile.jpg");
            form.Add(new StringContent(use_alphamatting.ToString().ToLower()), "use_alphamatting");
            var res = await Devmasters.Net.HttpClient.Simple.PostAsync<Base64ImageDataResponse>(this.ApiServerRoot + "v1/image/removebackground",
                form);

            if (res?.success == true)
            {
                return res.imageData();

            }
            else
                throw new ApiResponseException(res.error);
            }
            catch (Exception e)
            {

                throw new ApiResponseException(e.ToString());
            }

        }

        public  async Task<byte[]> ImageSuperResolutionAsync(byte[] bytesContent)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytesContent), "image", "anyfile.jpg");
            var res = await Devmasters.Net.HttpClient.Simple.PostAsync<Base64ImageDataResponse>(this.ApiServerRoot + "v1/image/superresolution",
                form);

            if (res?.success == true)
            {
                return res.imageData();

            }
            else
                throw new ApiResponseException(res.error);
        }

        public  async Task<string> VisionFaceRegisterAsync(byte[] bytesContent, string userid)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytesContent), "imageN", "anyfile.jpg");
            form.Add(new StringContent(userid), "userid");
            var res = await Devmasters.Net.HttpClient.Simple.PostAsync<VisionFaceRegisterResponse>(this.ApiServerRoot + "v1/vision/face/register",
                form);
            Console.WriteLine("S");

            if (res?.success == true)
            {
                return res.Message;

            }
            else
                throw new ApiResponseException(res.error);
        }

        public  async Task<string[]> VisionFaceListAsync()
        {
            var res = await Devmasters.Net.HttpClient.Simple.PostAsync<VisionFaceListResponse>(this.ApiServerRoot + "v1/vision/face/list");
            if (res?.success == true)
            {
                return res.faces;

            }
            else
                throw new ApiResponseException(res.error);
        }




        public class ResponseBase
        {
            public bool success { get; set; }
            public string error { get; set; }
            public int code { get; set; }
        }
        public class VisionFaceListResponse : ResponseBase
        {
            public string[] faces { get; set; }
        }
        public class VisionFaceRegisterResponse : ResponseBase
        {
            public string Message { get; set; }
        }

        public class PortraitFilterResponse : Base64ImageDataResponse
        {
            public string filtered_image { get; set; }
            public override byte[] imageData()
            {
                if (string.IsNullOrEmpty(filtered_image))
                    return null;
                return Convert.FromBase64String(filtered_image);
            }
        }
            public class Base64ImageDataResponse : ResponseBase
        {
            public string imageBase64 { get; set; }
            public virtual byte[] imageData()
            {
                if (string.IsNullOrEmpty(imageBase64))
                    return null;
                return Convert.FromBase64String(imageBase64);
            }
        }


        public class VisionFaceRecognizeResponse
        {
            public string success { get; set; }
            public Prediction[] predictions { get; set; }

            public class Prediction
            {
                public int x_max { get; set; }
                public int x_min { get; set; }
                public int y_min { get; set; }
                public int y_max { get; set; }
                public int confidence { get; set; }
                public string label { get; set; }
            }
        }
    }
}


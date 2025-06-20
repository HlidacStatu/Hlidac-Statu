namespace HlidacStatuApi.Code
{
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class TextPlainInputFormatter : TextInputFormatter
    {
        private const string contextType= "text/plain";

        public TextPlainInputFormatter()
        {
            // Add supported media type
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(contextType));

            // Add supported encodings
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
            SupportedEncodings.Add(Encoding.ASCII);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context, Encoding encoding)
        {
            var request = context.HttpContext.Request;

            // Ensure the encoding is valid
            if (!request.ContentType.Contains(contextType))
            {
                context.ModelState.TryAddModelError(context.ModelName, $"Unsupported encoding: {contextType}");
                return await InputFormatterResult.FailureAsync();
            }

            using var reader = new StreamReader(request.Body, encoding);
            string content = await reader.ReadToEndAsync();
            return await InputFormatterResult.SuccessAsync(content);
        }
    }
}

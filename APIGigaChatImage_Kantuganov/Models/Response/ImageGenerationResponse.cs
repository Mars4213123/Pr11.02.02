using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage_Kantuganov.Models.Response
{
    public class ImageGenerationResponse
    {
        public long created { get; set; }
        public List<ImageData> data { get; set; }
    }

    public class ImageData
    {
        public string url { get; set; }
        public string b64_json { get; set; }
    }
}

using Newtonsoft.Json;
using System;

namespace UploadBlobStorage.ViewModel
{
    public class ImagemViewModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public Guid ImagemId { get; set; }
        public string PathOriginal { get; set; }
        public string PathResize { get; set; }
        public string PathThumbs { get; set; }
        public DateTime Date { get; set; }
    }
}

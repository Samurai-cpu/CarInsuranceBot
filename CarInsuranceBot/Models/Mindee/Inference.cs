using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CarInsuranceBot.Models.Mindee
{
    public class Inference<T>
    {
        [JsonPropertyName("prediction")]
        public T? Prediction { get; set; }
    }
}

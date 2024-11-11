using Newtonsoft.Json;

namespace plsensors
{
    public class KerbsideSensor
    {
        public enum Status
        {
            Unoccupied,
            Present
        }

        [JsonProperty("kerbsideid")]
        public required int KerbsideId { get; set; }
        [JsonProperty("location")]
        public required Location Location { get; set; }
        [JsonProperty("zone_number")]
        public int? ZoneNumber { get; set; }
        [JsonProperty("lastupdated")]
        public DateTime LastUpdated { get; set; }
        [JsonProperty("status_timestamp")]
        public DateTime StatusTimestamp { get; set; }
        [JsonProperty("status_description")]
        public string StatusDescription { get; set; }

        public KerbsideSensor()
        {
            LastUpdated = DateTime.Now;
            StatusTimestamp = DateTime.Now;
            StatusDescription = "Unoccupied";
        }

        public void UpdateStatus(Status status)
        {
            LastUpdated = DateTime.Now;
            StatusTimestamp = DateTime.Now;
            StatusDescription = status.ToString();
        }
    }
}

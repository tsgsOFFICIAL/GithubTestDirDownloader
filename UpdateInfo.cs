using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubTestDirDownloader
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("changelog")]
        public string? Changelog { get; set; }
        [JsonPropertyName("historic_versions")]
        public List<HistoricVersion>? HistoricVersions { get; set; }

        /// <summary>
        /// Represents historical version information.
        /// </summary>
        public class HistoricVersion
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            [JsonPropertyName("changelog")]
            public string? Changelog { get; set; }
        }
    }
}

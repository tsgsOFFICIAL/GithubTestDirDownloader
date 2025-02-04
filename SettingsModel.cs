using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubTestDirDownloader
{
    internal class SettingsModel
    {
        [JsonPropertyName("window_width")]
        public double? WindowWidth { get; set; }
        [JsonPropertyName("window_height")]
        public double? WindowHeight { get; set; }

        public SettingsModel()
        {

        }

        public SettingsModel(double windowWidth, double windowHeight)
        {
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }
    }
}

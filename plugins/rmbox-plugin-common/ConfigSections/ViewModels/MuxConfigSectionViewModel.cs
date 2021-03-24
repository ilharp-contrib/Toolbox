﻿using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Ruminoid.Toolbox.Plugins.Common.ConfigSections.Views;

namespace Ruminoid.Toolbox.Plugins.Common.ConfigSections.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MuxConfigSectionViewModel : ReactiveObject
    {
        #region Constructor

        public MuxConfigSectionViewModel(
            MuxConfigSection view,
            JToken sectionConfig)
        {
            _view = view;
        }

        #endregion

        #region View

        private readonly MuxConfigSection _view;

        #endregion

        #region Config

        #endregion

        #region Data

        [JsonProperty("video")]
        private string _video = "";

        public string Video
        {
            get => _video;
            set => this.RaiseAndSetIfChanged(ref _video, value);
        }

        [JsonProperty("audio")]
        private string _audio = "";

        public string Audio
        {
            get => _audio;
            set => this.RaiseAndSetIfChanged(ref _audio, value);
        }

        [JsonProperty("subtitle")]
        private string _subtitle = "";

        public string Subtitle
        {
            get => _subtitle;
            set => this.RaiseAndSetIfChanged(ref _subtitle, value);
        }

        [JsonProperty("output")]
        private string _output = "";

        public string Output
        {
            get => _output;
            set => this.RaiseAndSetIfChanged(ref _output, value);
        }

        #endregion

        #region Operations

        public async Task PickFile(string field)
        {
            string title = field switch
            {
                "Video" => "视频",
                "Audio" => "音频",
                "Subtitle" => "字幕",
                _ => ""
            };

            OpenFileDialog dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = $"选择{title}文件"
            };
            string[] result = await dialog.ShowAsync((Window)_view.GetVisualRoot());

            if (!result.Any() ||
                string.IsNullOrWhiteSpace(result[0]))
                return;

            switch (field)
            {
                case "Video":
                    Video = result[0];
                    break;
                case "Audio":
                    Audio = result[0];
                    break;
                case "Subtitle":
                    Subtitle = result[0];
                    break;
            }
        }

        public async Task SaveFile()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "选择输出文件",
                DefaultExtension = ".mp4"
            };

            var result = await dialog.ShowAsync((Window)_view.GetVisualRoot());

            if (string.IsNullOrWhiteSpace(result)) return;

            Output = result;
        }

        #endregion
    }
}
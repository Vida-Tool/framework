using System.Collections.Generic;
using System;
namespace Vida.Framework.Editor
{
    [Serializable]
    public class VidaAssetCollection
    {
        public string Name;
        public List<string> Templates { get; set; }
        public string Info { get; set; }
        public string Location { get; set; }
        public string DownloadLocation { get; set; }
        public string Menu { get; set; }

        public string[] SeparatedMenu => Menu.Split('/');
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCMDecrypter
{
    internal class MusicMetaInfo
    {
        public string? musicId {get;set;}
        public string? musicName {get;set;}
        public List<List<string>>? artist {get;set;}
        public string? albumId {get;set;}
        public string? album { get;set;}
        public string? albumPicId {get;set;}
        public string? albumPic { get;set;} 
        public int? bitrate {get;set;}
        public string? mp3DocId { get;set;}
        public int? duration {get;set;}
        public string? mvId {get;set;}
        public string? format {get;set;}
        public int? fee {get;set;}
        public List<string>? transNames {get;set;}  
        public List<string>? alias {get;set;}
    }
}

using System.Collections.Generic;

namespace YandexAPI
{
    
    
    public class AlbumRoot
    {
        public class VolumesItemItem
    {
    
        /// <summary>
        /// 
        /// </summary>
        public string id { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string realId { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string title { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string version { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public Major major { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool available { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableForPremiumUsers { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableFullWithoutPermission { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int durationMs { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string storageDir { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int fileSize { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public Normalization normalization { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int previewDurationMs { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <ArtistsItem > artists { get; set; }    

        public List <AlbumsItem > albums { get; set; }    

        public string coverUri { get; set; }    

        public string ogImage { get; set; }    

        public bool lyricsAvailable { get; set; }    

        public bool best { get; set; }    

        public string type { get; set; }    

        public bool rememberPosition { get; set; }
    }
        public class Normalization
        {
            public double gain { get; set; }
            public int peak { get; set; }
        }
        public class Major
        {
            public int id { get; set; }
            public string name { get; set; }
        }
        public class LabelsItem
        {
            public int id { get; set; }
            public string name { get; set; }
        }
        public class DuplicatesItem
        {
            public int id { get; set; }
            public string title { get; set; }
            public int year { get; set; }
            public string releaseDate { get; set; }
            public string coverUri { get; set; }
            public string ogImage { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public string genre { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public List <string > buy { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public int trackCount { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool recent { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool veryImportant { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public List <ArtistsItem > artists { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public List <LabelsItem > labels { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool available { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool availableForPremiumUsers { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool availableForMobile { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool availablePartially { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public List <int > bests { get; set; }
        }
        public class AlbumsItem
    {
    
        /// <summary>
        /// 
        /// </summary>
        public bool redirected { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string volumes { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > prerolls { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <DuplicatesItem > duplicates { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <int > bests { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availablePartially { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableForMobile { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableForPremiumUsers { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool available { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <LabelsItem > labels { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <ArtistsItem > artists { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool veryImportant { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool recent { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int trackCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > buy { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string genre { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string ogImage { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string coverUri { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string releaseDate { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int year { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string title { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
    }
        public class ArtistsItem
        {
    
            /// <summary>
            /// 
            /// </summary>
            public int id { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool various { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public bool composer { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public Cover cover { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public List <string > genres { get; set; }
        }
        public class Cover
        {
    
            /// <summary>
            /// 
            /// </summary>
            public string type { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public string prefix { get; set; }    
            /// <summary>
            /// 
            /// </summary>
            public string uri { get; set; }
        }
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string title { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int year { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string releaseDate { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string coverUri { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string ogImage { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string genre { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > buy { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int trackCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool recent { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool veryImportant { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <ArtistsItem > artists { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <LabelsItem > labels { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool available { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableForPremiumUsers { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availableForMobile { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool availablePartially { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <int > bests { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <DuplicatesItem> duplicates { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > prerolls { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <List <VolumesItemItem > > volumes { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool redirected { get; set; }
    }
}
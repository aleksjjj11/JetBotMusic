using System.Collections.Generic;

namespace YandexAPI
{
    public class TracksItem
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
        /// <summary>
        /// 
        /// </summary>
        public List <AlbumsItem > albums { get; set; }    
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
        public bool lyricsAvailable { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string type { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool rememberPosition { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool embedPlayback { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string prefix { get; set; }
    }
}
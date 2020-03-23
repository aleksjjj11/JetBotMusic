using System.Collections.Generic;

namespace YandexAPI
{
    public class AlbumsItem
    {
    
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
        public string version { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string contentWarning { get; set; }    
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
        public TrackPosition trackPosition { get; set; }
    }
}
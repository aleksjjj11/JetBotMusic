using System.Collections.Generic;

namespace YandexAPI
{
    public class SimilarPlaylistsItem
    {
    
        /// <summary>
        /// 
        /// </summary>
        public Owner owner { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool available { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int uid { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int kind { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string title { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string description { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string descriptionFormatted { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int revision { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int snapshot { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int trackCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string visibility { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool collective { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string created { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string modified { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool isBanner { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool isPremiere { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int durationMs { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public Cover cover { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string ogImage { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List<TagsItem> tags { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int likesCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > prerolls { get; set; }
    }
}
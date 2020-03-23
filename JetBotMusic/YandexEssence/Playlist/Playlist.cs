using System.Collections.Generic;

namespace YandexAPI
{
    public class Playlist
    {
    
        /// <summary>
        /// 
        /// </summary>
        public int revision { get; set; }    
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
        public int trackCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string visibility { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public Cover cover { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public Owner owner { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <TracksItem > tracks { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string modified { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <int> trackIds { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public string ogImage { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <TagsItem > tags { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int likesCount { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public int duration { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <SimilarPlaylistsItem > similarPlaylists { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool collective { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public List <string > prerolls { get; set; }    
        /// <summary>
        /// 
        /// </summary>
        public bool doNotIndex { get; set; }
    }
}
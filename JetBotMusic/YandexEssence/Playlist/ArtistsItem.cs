using System.Collections.Generic;

namespace YandexAPI
{
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
}
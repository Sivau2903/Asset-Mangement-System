using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    
        public class BookUploadViewModel
        {
            public int SlNo { get; set; }                 
            public string AccNo { get; set; }          
            public string CallNo { get; set; }          
            public string Title { get; set; }           
            public string Author { get; set; }         
            public string PlaceOfPublisher { get; set; } 
            public int? Year { get; set; }              
            public string Edition { get; set; }         
            public string Pages { get; set; }          
            public string Volume { get; set; }          
            public string Source { get; set; }          
            public decimal? Price { get; set; }          
        }

    }

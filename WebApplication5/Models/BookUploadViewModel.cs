using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    
        public class BookUploadViewModel
        {
            public int SlNo { get; set; }                 // Serial Number
            public string AccNo { get; set; }            // Accession Number
            public string CallNo { get; set; }           // Call Number
            public string Title { get; set; }            // Title of the Book
            public string Author { get; set; }           // Author Name
            public string PlaceOfPublisher { get; set; } // Place of Publishers
            public int? Year { get; set; }               // Year
            public string Edition { get; set; }          // Edition
            public string Pages { get; set; }            // Pages
            public string Volume { get; set; }           // Volume
            public string Source { get; set; }           // Source
            public decimal? Price { get; set; }          // Price
        }

    }

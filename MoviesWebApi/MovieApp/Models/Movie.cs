using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieApp.Models
{
    public class Movie
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
            public string Thumbnail { get; set; }
        public int Category { get; set; }
        public int ReleaseYear { get; set; }
        public double AverageRating { get; set; }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieApp.Models
{
    public class MovieRating
    {
        public int ID { get; set; }
        public int MovieID { get; set; }
        public int UserID { get; set; }
        public int UserRating { get; set; }
    }
}

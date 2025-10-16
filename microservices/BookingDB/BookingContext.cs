using BookingDB.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingDB
{
    public class BookingContext : DbContext
    {
        public DbSet<Booking> Bookings { get; set; } 

        public BookingContext(DbContextOptions<BookingContext> options) : base(options) { }
    }
}

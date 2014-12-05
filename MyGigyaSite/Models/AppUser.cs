using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGigyaSite.Models
{
    public class AppUser
    {
        [Key]
        public string AppUserID { get; set; }

        [Required]
        [Display(Name="Email Address")]
        public string Email { get; set; }

        [Display(Name="Password")]
        public string Password { get; set; }
    }
}

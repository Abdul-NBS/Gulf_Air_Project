using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace eBriefingWebApp.Models
{
            public class SingleFileModel : ReponseModel
        {
            [Required(ErrorMessage = "Please enter file name")]
            public string FileName { get; set; }
            [Required(ErrorMessage = "Please select file")]
            public IFormFile File { get; set; }
        }
    
}
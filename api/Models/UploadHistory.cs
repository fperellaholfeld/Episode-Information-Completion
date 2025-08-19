using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace api.Models
{

    public enum UploadStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public class UploadHistory
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(255)]
        public string FilePath { get; set; }
        public DateTime? CreatedTimestamp { get; set; }
    }
}
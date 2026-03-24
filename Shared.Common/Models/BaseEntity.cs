using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Models
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdateAt {  get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

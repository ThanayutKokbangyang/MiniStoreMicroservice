using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Models
{
    public class Order : BaseEntity
    {
        public int UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string ShippingAddress { get; set; } = string.Empty;
    }
}

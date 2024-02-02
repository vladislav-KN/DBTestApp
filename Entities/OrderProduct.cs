using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class OrderProduct
    {
        public int? OrderNo { get { return Order?.No; } }
        [ExcludeFromParametersAttribute]
        public Order? Order { get; set; }
        public string? ProductName { get { return Product?.Name; } }
        [ExcludeFromParametersAttribute]
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Order
    {
        public int No {  get; set; }
        public float Sum { get; set; }
        public DateTime RegDate { get; set; }
        public string? UserEmail { get { return User?.Email; } }
        [ExcludeFromParametersAttribute]
        public User? User { get; set; }

    }
}

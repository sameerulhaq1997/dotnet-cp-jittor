using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jittor.App.Models
{
    public class TableColumns
    {
        public string Field { get; set; }
        public string HeaderName { get; set; }
        public string TableName { get; set; }
        public bool Hideable { get; set; }
        public bool Hide { get; set; }
    }
}

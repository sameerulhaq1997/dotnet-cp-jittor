using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Jittor.App.DataServices.FrameworkRepository;

namespace Jittor.App.DataServices.CustomEntities
{
    public class EventLogEntity : EventLogs
    {
        public object OldRecordEntity { get; set; }
        public object ChangesEntity { get; set; }

    }
}

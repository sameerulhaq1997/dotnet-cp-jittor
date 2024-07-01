using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MacroEconomics.Shared.DataServices.FrameworkRepository;

namespace MacroEconomics.Shared.DataServices.CustomEntities
{
    public class EventLogEntity : EventLogs
    {
        public object OldRecordEntity { get; set; }
        public object ChangesEntity { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jittor.App.DataServices
{
    public enum ConnectionType
    {
        SCConnection,
        CPConnection,
        ArgaamConnection
    }

    public static class ConnectionStrings
    {
        private static readonly Dictionary<ConnectionType, string> connections = new Dictionary<ConnectionType, string>
    {
        { ConnectionType.SCConnection, "Data Source=172.16.2.44;Initial Catalog=JittorCP_01July2024;Persist Security Info=True;User ID=argplus_dbuser;Password=Argaam32!" },
        { ConnectionType.CPConnection, "Data Source=172.16.2.44;Initial Catalog=ArgaamNext_CMS;Persist Security Info=True;User ID=argplus_dbuser;Password=Argaam32!" },
        { ConnectionType.ArgaamConnection, "Data Source=172.16.2.44;Initial Catalog=ArgaamPlus_29JAN2024;Persist Security Info=True;User ID=argplus_dbuser;Password=Argaam32!" }
    };

        public static string GetConnectionString(ConnectionType connectionType)
        {
            return connections[connectionType];
        }
    }
}

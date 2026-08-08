using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Data.CommonClass
{
    public class CurrentUser
    {
        public long user_code { get; set; }
        public string user_name { get; set; }
        public string mobile_no { get; set; }
        public string email { get; set; }
        public string user_type { get; set; }
    }
}

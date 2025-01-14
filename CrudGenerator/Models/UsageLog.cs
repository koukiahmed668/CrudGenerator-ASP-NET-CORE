using System;
using System.Collections.Generic;

namespace CrudGenerator.Models
{
    public class UsageLog
    {
        public int Id { get; set; }
        public string UserIp { get; set; }
        public DateTime Timestamp { get; set; }

        public List<string> GeneratedModels { get; set; }
        public string ResponseType { get; set; }

        public List<string> Roles { get; set; }  // Change to List<string>

        public bool JwtIncluded { get; set; }
    }
}

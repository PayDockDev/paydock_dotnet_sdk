﻿using System.Collections.Generic;

namespace Paydock_dotnet_sdk.Models
{
    public class FraudData : FraudBase
    {
        public string token { get; set; }
        public dynamic data { get; set; }
    }
}

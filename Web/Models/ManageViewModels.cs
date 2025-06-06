﻿using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace HlidacStatu.Web.Models
{
    public class IndexViewModel
    {
        public bool HasWatchdogs { get; set; }
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
    }
    
}
﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class DatabaseOptions
    {
        [Required] public string ConnectionString { get; set; }
    }
}

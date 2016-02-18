﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspnetTool.Commands
{
    public class AuthenticatedOption
    {
        [Option('t', "token", HelpText = "The OAuth token to use for authentication.")]
        public string Token { get; set; }
    }
}

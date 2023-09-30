﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGbot
{
    internal class Todo
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsDone { get; set; }

        public Todo(string name)
        {
            Name = name;
            Id = Guid.NewGuid().ToString();
            IsDone = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgTaskBot
{
    internal class Todo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDone { get; set; }
        

        public Todo(string name)
        {
            Name = name;
            Id = Guid.NewGuid().ToString();
            IsDone = false;
        }

        public Todo() 
        { 

        }

    }
}

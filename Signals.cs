using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow_ClientWin
{
    public class Signal<T> where T : SignalProvider
    {
        public TimeSpan Age { get; set; }
        public string Path { get; set; }
        public string Value { get; set; }
    }

    public abstract class SignalProvider
    {
        
    }
}

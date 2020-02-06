using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechConsole
{
    class Substitution
    {
        public List<string> matches;
        public List<string> replacement;

        public Substitution(List<string> matches, List<string> replacement) {
            this.matches = matches;
            this.replacement = replacement;
        }
    }
}

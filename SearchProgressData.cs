using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymWorkoutTracker
{
    public class SearchProgressData
    {
        public bool ClearResults { get; set; }
        public bool ResetProgressBar { get; set; }
        public string ResultToAdd { get; set; }
        public string Message { get; set; }
    }
}

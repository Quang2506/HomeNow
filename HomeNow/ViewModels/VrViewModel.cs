using System.Collections.Generic;
using Core.Models; 

namespace HomeNow.ViewModels
{
    public class VrViewModel
    {
        public int PropertyId { get; set; }
        public string Lang { get; set; }
        public IEnumerable<VrScene> Scenes { get; set; }
    }
}

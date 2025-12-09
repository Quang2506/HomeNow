using System.Collections.Generic;
using Core.Models;   // để dùng VrScene

namespace HomeNow.ViewModels
{
    public class VrViewModel
    {
        public int PropertyId { get; set; }
        public string Lang { get; set; }
        public IEnumerable<VrScene> Scenes { get; set; }
    }
}

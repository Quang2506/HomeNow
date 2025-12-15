using System.Collections.Generic;
using Core.Models;

namespace HomeNow.ViewModels
{
    public class PropertyListPageViewModel
    {
        public string Mode { get; set; } 

        public int? CityId { get; set; }
        public string PriceRange { get; set; }
        public string PropertyType { get; set; }
        public string Keyword { get; set; }

        public List<PropertyListItemViewModel> Items { get; set; } = new List<PropertyListItemViewModel>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 16;
        public int TotalPages { get; set; } = 1;

        public string Title { get; set; }
    }
}

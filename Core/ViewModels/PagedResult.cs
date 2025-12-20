using System.Collections.Generic;

namespace Core.ViewModels
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalItems { get; set; }

        public PagedResult()
        {
            Items = new List<T>();
        }
        public int Total
        {
            get => TotalItems;
            set => TotalItems = value;
        }
    }
}

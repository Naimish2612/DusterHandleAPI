using System;
using System.Collections.Generic;
using System.Text;

namespace DUSTER.EComm.Data.Helpers.Pagination
{
    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public PaginationMetadata Metadata { get; set; } = new();
    }

    public class PaginationParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

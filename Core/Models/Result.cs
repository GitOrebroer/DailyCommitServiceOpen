using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public int HttpStatusCode { get; set; } = 500;
        public T? Value { get; set; }
        public Result(T value) => this.Value = value;
        public Result()
        {

        }
    }
}

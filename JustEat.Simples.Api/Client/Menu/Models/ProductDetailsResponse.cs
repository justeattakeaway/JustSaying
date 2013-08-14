using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu.Models
{
    public class ProductDetailsResponse
    {
        public IEnumerable<ProductDetails> ProductDetails { get; set; }
        public int MenuCardId { get; set; }
    }
}

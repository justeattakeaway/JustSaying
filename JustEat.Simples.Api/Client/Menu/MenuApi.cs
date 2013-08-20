using JustEat.Simples.Api.Client.Menu.Models;
using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu
{
    public class MenuApi : ApiClientBase, IMenuApi
    {
        public MenuApi(IApiSettings apiSettings)
            : base(apiSettings)
        {
        }

        public ProductDetailsResponse GetProductDetails(string menuId, IEnumerable<string> productIds)
        {
            var url = BuildUrl(Operations.Menu, menuId, string.Join(",", productIds));

            var result = GetJson<ProductDetailsResponse>(url);

            return result;
        }

        private static class Operations
        {
            public const string Menu = "/menu/{menuId}/products?productIds={productIds}";
        }
    }
}

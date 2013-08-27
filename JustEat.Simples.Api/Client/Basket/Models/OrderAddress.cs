namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class OrderAddress
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string Email { get; set; }
        public bool Orderable { get; set; }
        public string PhoneNumber { get; set; }
        public string PostCode { get; set; }
    }
}
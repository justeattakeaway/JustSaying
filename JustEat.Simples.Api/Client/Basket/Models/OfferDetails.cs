namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class OfferDetails
    {
        public int Id { get; set; }
        public decimal Discount { get; set; }
        public string DiscountType { get; set; }
        public decimal QualifyingValue { get; set; } //this comes from OfferApi
    }
}
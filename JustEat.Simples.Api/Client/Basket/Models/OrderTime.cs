using System;

namespace JustEat.Simples.Api.Client.Basket.Models
{
    public class OrderTime
    {
        public bool Asap { get; set; }
        public DateTime DateTime { get; set; }
        public bool Orderable { get; set; }
    }
}
using NServiceBus;

namespace Messages
{
    public class ItemAddedOrUpdated : IEvent
    {
        public string OrderId { get; set; }
        public Filling Filling { get; set; }
        public int Quantity { get; set; }
    }
}
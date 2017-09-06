using NServiceBus;

namespace Messages
{
    public class AddOrUpdateItem : IMessage
    {
        public string OrderId { get; set; }
        public Filling Filling { get; set; }
        public int Quantity { get; set; }
    }
}
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [ChildOf]
    public partial class Item : Entity,IAwake<int>,IDestroy,ISerializeToEntity
    {
        public int ConfigId { get; set; }
        
        public int ContainerType { get; set; }
        
        public int Count { get; set; }
    }
}
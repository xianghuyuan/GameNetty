using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [ComponentOf(typeof(MailUnit))]
    public class MailComponent:Entity,IAwake,IDestroy,IDeserialize
    {
        [BsonIgnore]
        public List<EntityRef<MailInfoEntity>> MailInfosList = new List<EntityRef<MailInfoEntity>>();
    }
}
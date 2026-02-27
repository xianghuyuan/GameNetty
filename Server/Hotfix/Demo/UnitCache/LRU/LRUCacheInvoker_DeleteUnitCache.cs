namespace ET.Server
{
    [Invoke((long)(SceneType.UnitCache))]
    public class LRUCacheInvoker_DeleteUnitCache: AInvokeHandler<LRUUnitCacheDelete>
    {
        public override void Handle(LRUUnitCacheDelete args)
        {
            LRUCache lruCache = args.LruCache;
            lruCache?.GetParent<UnitCacheComponent>().Delete(args.key).Coroutine();
        }
    }
}
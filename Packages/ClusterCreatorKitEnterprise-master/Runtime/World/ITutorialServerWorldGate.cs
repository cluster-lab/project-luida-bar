namespace ClusterVR.CreatorKit.World
{
    /// <summary>
    /// 入室時にチュートリアル用サーバーの設定が必要であることを示す、WorldGateに横付けでアタッチされるinterface
    /// </summary>
    public interface ITutorialServerWorldGate
    {
        IWorldGate WorldGate { get; }
    }
}

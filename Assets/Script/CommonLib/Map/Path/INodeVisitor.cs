namespace Script.CommonLib
{
    /// <summary>
    /// return값이 true일 때 순회를 종료
    /// </summary>
    public delegate bool BreakablePointVisitor(int x, int y);
    
    public interface INodeVisitor
    {
        /// <summary>
        /// 두 Node 사이를 순차적으로 방문합니다.
        /// BreakablePointVisitor의 return값이 true일 때 순회를 종료합니다.
        /// </summary>
        public bool VisitPath(GridPos startNode, GridPos endNode, BreakablePointVisitor breakablePointVisitor);
    }
}

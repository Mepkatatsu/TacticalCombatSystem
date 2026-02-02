using System.Collections.Generic;

namespace Script.CommonLib.Tests
{
    public class NodeVisitorTest : ITest
    {
        private readonly List<GridPos> _points = new List<GridPos>();

        public bool Test()
        {
            var visitor = new BresenhamSuperCoverNodeVisitor();

            if (!TestHorizontalLine(visitor))
                return false;

            if (!TestHorizontalLineReverse(visitor))
                return false;
            
            if (!TestVerticalLine(visitor))
                return false;

            if (!TestVerticalLineReverse(visitor))
                return false;

            if (!TestDiagonalLine45Degrees(visitor))
                return false;
            
            if (!TestDiagonalLine45DegreesReverse(visitor))
                return false;
            
            if (!TestDiagonalLineNon45DegreesLowSlope(visitor))
                return false;

            if (!TestDiagonalLineNon45DegreesLowSlopeReverse(visitor))
                return false;

            if (!TestDiagonalLineNon45DegreesHighSlope(visitor))
                return false;
            
            if (!TestDiagonalLineNon45DegreesHighSlopeReverse(visitor))
                return false;
            
            return true;
        }

        private bool TestHorizontalLine(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 0);
            var end = new GridPos(5, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(1, 0),
                new GridPos(2, 0),
                new GridPos(3, 0),
                new GridPos(4, 0),
                new GridPos(5, 0),
            };

            return TestLine(visitor, start, end, expected, nameof(TestHorizontalLine));
        }
        
        private bool TestHorizontalLineReverse(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(5, 0);
            var end = new GridPos(0, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(5, 0),
                new GridPos(4, 0),
                new GridPos(3, 0),
                new GridPos(2, 0),
                new GridPos(1, 0),
                new GridPos(0, 0),
            };

            return TestLine(visitor, start, end, expected, nameof(TestHorizontalLineReverse));
        }
        
        private bool TestVerticalLine(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 0);
            var end = new GridPos(0, 5);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(0, 1),
                new GridPos(0, 2),
                new GridPos(0, 3),
                new GridPos(0, 4),
                new GridPos(0, 5),
            };

            return TestLine(visitor, start, end, expected, nameof(TestVerticalLine));
        }
        
        private bool TestVerticalLineReverse(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 5);
            var end = new GridPos(0, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 5),
                new GridPos(0, 4),
                new GridPos(0, 3),
                new GridPos(0, 2),
                new GridPos(0, 1),
                new GridPos(0, 0),
            };

            return TestLine(visitor, start, end, expected, nameof(TestVerticalLineReverse));
        }
        
        private bool TestDiagonalLine45Degrees(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 0);
            var end = new GridPos(5, 5);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(1, 1),
                new GridPos(2, 2),
                new GridPos(3, 3),
                new GridPos(4, 4),
                new GridPos(5, 5),
                
                // 45도로 이동할 때는 오른쪽 칸, 위 칸도 포함
                new GridPos(1, 0),
                new GridPos(2, 1),
                new GridPos(3, 2),
                new GridPos(4, 3),
                new GridPos(5, 4),
                
                // 45도로 이동할 때는 오른쪽 칸, 위 칸도 포함
                new GridPos(0, 1),
                new GridPos(1, 2),
                new GridPos(2, 3),
                new GridPos(3, 4),
                new GridPos(4, 5),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLine45Degrees));
        }
        
        private bool TestDiagonalLine45DegreesReverse(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(5, 5);
            var end = new GridPos(0, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(5, 5),
                new GridPos(4, 4),
                new GridPos(3, 3),
                new GridPos(2, 2),
                new GridPos(1, 1),
                new GridPos(0, 0),
                
                // 45도로 이동할 때는 오른쪽 칸, 위 칸도 포함
                new GridPos(1, 0),
                new GridPos(2, 1),
                new GridPos(3, 2),
                new GridPos(4, 3),
                new GridPos(5, 4),
                
                // 45도로 이동할 때는 오른쪽 칸, 위 칸도 포함
                new GridPos(0, 1),
                new GridPos(1, 2),
                new GridPos(2, 3),
                new GridPos(3, 4),
                new GridPos(4, 5),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLine45DegreesReverse));
        }
        
        private bool TestDiagonalLineNon45DegreesLowSlope(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 0);
            var end = new GridPos(5, 1);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(1, 0),
                new GridPos(2, 0),
                new GridPos(2, 1),
                new GridPos(3, 0),
                new GridPos(3, 1),
                new GridPos(4, 1),
                new GridPos(5, 1),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLineNon45DegreesLowSlope));
        }
        
        private bool TestDiagonalLineNon45DegreesLowSlopeReverse(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(5, 1);
            var end = new GridPos(0, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(5, 1),
                new GridPos(4, 1),
                new GridPos(3, 1),
                new GridPos(3, 0),
                new GridPos(2, 1),
                new GridPos(2, 0),
                new GridPos(1, 0),
                new GridPos(0, 0),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLineNon45DegreesLowSlopeReverse));
        }
        
        private bool TestDiagonalLineNon45DegreesHighSlope(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(0, 0);
            var end = new GridPos(1, 5);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(0, 1),
                new GridPos(0, 2),
                new GridPos(1, 2),
                new GridPos(0, 3),
                new GridPos(1, 3),
                new GridPos(1, 4),
                new GridPos(1, 5),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLineNon45DegreesHighSlope));
        }
        
        private bool TestDiagonalLineNon45DegreesHighSlopeReverse(BresenhamSuperCoverNodeVisitor visitor)
        {
            var start = new GridPos(1, 5);
            var end = new GridPos(0, 0);
            
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            var expected = new HashSet<GridPos>
            {
                new GridPos(1, 5),
                new GridPos(1, 4),
                new GridPos(1, 3),
                new GridPos(0, 3),
                new GridPos(1, 2),
                new GridPos(0, 2),
                new GridPos(0, 1),
                new GridPos(0, 0),
            };

            return TestLine(visitor, start, end, expected, nameof(TestDiagonalLineNon45DegreesHighSlopeReverse));
        }
        
        private bool TestLine(BresenhamSuperCoverNodeVisitor visitor, GridPos start, GridPos end, HashSet<GridPos> excepted, string testName)
        {
            _points.Clear();
            
            visitor.VisitPath(start, end, GetPoints);

            if (_points.Count != excepted.Count)
            {
                LogHelper.Error($"{testName} failed. (_points.Count != excepted.Count)");
                return false;
            }
            
            for (var i = 0; i < _points.Count; i++)
            {
                if (!excepted.Contains(_points[i]))
                {
                    LogHelper.Error($"{testName} failed. (!excepted.Contains(_points[i])");
                    return false;
                }
            }

            return true;
        }

        private bool GetPoints(int x, int y)
        {
            _points.Add(new GridPos(x, y));
            return false;
        }
    }
}

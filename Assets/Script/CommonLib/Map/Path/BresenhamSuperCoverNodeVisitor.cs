namespace Script.CommonLib
{
    // 서버 부담을 줄이기 위해 가벼운 연산만 사용하는 Bresenham 알고리즘을 변형하여 사용함
    public class BresenhamSuperCoverNodeVisitor : INodeVisitor
    {
        public bool VisitPath(GridPos startNode, GridPos endNode, BreakablePointVisitor breakablePointVisitor)
        {
            if (breakablePointVisitor == null)
                return true;

            int x = startNode.X;
            int y = startNode.Y;
            
            // for문과 계산 시 단위 통일을 위해 dx, dy 값을 절대값으로 사용
            CalculateStepAndAbsDelta(startNode, endNode, out int dx, out int dy, out int xStep, out int yStep);

            if (breakablePointVisitor(x, y))
                return false;

            // 2개 통일이 가능할 것 같은데 가독성을 위해 분리하여 사용함
            if (dx >= dy)
                return IsVisibleByLowSlope(dx, dy, xStep, yStep, x, y, breakablePointVisitor);
            else
                return IsVisibleByHighSlope(dx, dy, xStep, yStep, x, y, breakablePointVisitor);
        }

        private static void CalculateStepAndAbsDelta(GridPos startNode, GridPos endNode, out int dx, out int dy, out int xStep, out int yStep)
        {
            dx = endNode.X - startNode.X;
            dy = endNode.Y - startNode.Y;

            xStep = 1;
            yStep = 1;

            if (dx < 0)
            {
                xStep = -1;
                dx = -dx;
            }

            if (dy < 0)
            {
                yStep = -1;
                dy = -dy;
            }
        }
        
        private bool IsVisibleByLowSlope(int dx, int dy, int xStep, int yStep, int x, int y, BreakablePointVisitor breakablePointVisitor)
        {
            var error = dx; // 
            PrepareForSuperCover(dx, dy, error, out int ddx, out int ddy, out int errorPrev);

            // 기울기가 작은 경우, x좌표를 1칸씩 이동한다.
            for (int i = 0; i < dx; i++)
            {
                x += xStep;
                error += ddy;

                // x좌표가 충분히 움직였을 때 y좌표를 1칸 움직여준다.
                if (error > ddx)
                {
                    y += yStep;
                    error -= ddx;
                    
                    // y좌표가 바뀌면서 직선이 통과한 칸을 검사한다. 다른 칸들을 스치며 지나간 경우 두 칸 모두 검사한다.
                    if (error + errorPrev < ddx)
                    {
                        if (breakablePointVisitor(x, y - yStep))
                            return false;
                    }
                    else if (error + errorPrev > ddx)
                    {
                        if (breakablePointVisitor(x - xStep, y))
                            return false;
                    }
                    else
                    {
                        if (breakablePointVisitor(x, y - yStep))
                            return false;
                        
                        if (breakablePointVisitor(x - xStep, y))
                            return false;
                    }
                }

                if (breakablePointVisitor(x, y))
                    return false;
                
                errorPrev = error;
            }

            return true;
        }

        private bool IsVisibleByHighSlope(int dx, int dy, int xStep, int yStep, int x, int y, BreakablePointVisitor breakablePointVisitor)
        {
            var error = dy;
            PrepareForSuperCover(dx, dy, error, out int ddx, out int ddy, out int errorPrev);

            // 기울기가 큰 경우, y좌표를 1칸씩 이동한다.
            for (int i = 0; i < dy; i++)
            {
                y += yStep;
                error += ddx;

                // y좌표가 충분히 움직였을 때 x좌표를 1칸 움직여준다.
                if (error > ddy)
                {
                    x += xStep;
                    error -= ddy;

                    // x좌표가 바뀌면서 직선이 통과한 칸을 추가한다. 다른 칸들을 스치며 지나간 경우 두 칸 모두 추가한다.
                    if (error + errorPrev < ddy)
                    {
                        if (breakablePointVisitor(x - xStep, y))
                            return false;
                    }
                    else if (error + errorPrev > ddy)
                    {
                        if (breakablePointVisitor(x, y - yStep))
                            return false;
                    }
                    else
                    {
                        if (breakablePointVisitor(x - xStep, y))
                            return false;
                        
                        if (breakablePointVisitor(x, y - yStep))
                            return false;
                    }
                }

                if (breakablePointVisitor(x, y))
                    return false;
                
                errorPrev = error;
            }

            return true;
        }
        
        private void PrepareForSuperCover(int dx, int dy, int error, out int ddx, out int ddy, out int errorPrev)
        {
            // SuperCover를 위해 dx, dy를 2배로 늘려서 계산함 (나중에 error + errorPrev 계산을 정수로 하기 위함)
            ddx = dx * 2;
            ddy = dy * 2;
            errorPrev = error;
        }
    }
}

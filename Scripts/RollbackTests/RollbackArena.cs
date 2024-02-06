using System.Collections.Generic;
using System.Linq;

namespace ADK
{
    public partial class RollbackArena : Arena
    {
        List<FrameDrawData> drawHistory = new();
        int latestDrawnTick = 0;

        public List<Snake> DrawArchived(int tickNumber, List<Snake> aliveSnakes)
        {
            FrameDrawData drawData = GetDrawData(aliveSnakes);
            snakeComputer.Draw(drawData.SnakeDrawData, drawData.LineDrawData);
            drawHistory.Add(drawData);
            latestDrawnTick++;
            return GetCollisions(aliveSnakes);
        }

        public void Rollback(int tickNumber)
        {

        }

        // can never draw erase lines other than gaps while there is an unconfirmed predicted tick drawn,
        // because erasures cannot be rolled back over...
        List<LineData> GetEraseData(int tickNumber)
        {
            int eraseTick = latestDrawnTick;
            List<LineData> eraseData = new();
            while (eraseTick > tickNumber)
            {
                FrameDrawData drawDataToErase = drawHistory[^1];
                eraseData.AddRange(drawDataToErase.LineDrawData.Where(l => l.colorA > 0));
                //LineData eraseLine = 
                eraseTick--;
            }
            return null;
        }
    }
}
﻿namespace PacManDebugger
{
    public readonly struct HistoricEvent
    {
        public CellLocation OriginalLocation { get; }
        public CellLocation FinalLocation { get; }
        public bool WasMoveEvent { get; }

        public HistoricEvent(CellLocation originalLocation, CellLocation finalLocation,  bool wasMoveEvent)
        {
            OriginalLocation = originalLocation;
            FinalLocation = finalLocation;
            WasMoveEvent = wasMoveEvent;
        }
    }


}
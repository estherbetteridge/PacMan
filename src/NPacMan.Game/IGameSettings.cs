﻿using System.Collections.Generic;

namespace NPacMan.Game
{
    public interface IGameSettings
    {
        PacMan PacMan { get; }
        IReadOnlyCollection<CellLocation> Walls { get; }
        IReadOnlyCollection<CellLocation> Doors { get; }
        IReadOnlyCollection<CellLocation> GhostHouse { get; }
        IReadOnlyCollection<CellLocation> Coins { get; }
        IReadOnlyCollection<CellLocation> PowerPills { get; }
        IReadOnlyDictionary<CellLocation, CellLocation> Portals { get; }
        int Width { get; }
        int Height { get; }
        IReadOnlyCollection<Ghost> Ghosts { get; }
        CellLocation Fruit { get; }

        GameStatus InitialGameStatus { get; }
        int InitialLives { get; }

        int InitialScatterTimeInSeconds { get; }

        int ChaseTimeInSeconds { get; }

        int FrightenedTimeInSeconds { get; }
        
        IDirectionPicker DirectionPicker { get; }
        IReadOnlyCollection<int> FruitAppearsAfterCoinsEaten { get; }
        int FruitVisibleForSeconds { get; }

        int PointsNeededForBonusLife { get; }
    }
}
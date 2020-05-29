﻿using NPacMan.BotSDK;
using System.Collections.Generic;
using System.Linq;

namespace NPacMan.Bot
{
    internal class GreedyBot
    {
        private readonly Graph _graph;
        private BotGame _game = default!;

        public GreedyBot(BotBoard board) 
        {
            _graph = GraphBuilder.Build(board);
        }

        public Direction SuggestNextDirection(BotGame game)
        {
            _game = game;

            var currentLocation = _graph.GetNodeForLocation(_game.PacMan.Location);

            var shortestDistances = DistancesCalculator.CalculateDistances(_game, _graph, currentLocation);

            var nearestCoin = FindNearestCoin(_game.Coins, shortestDistances);

            Direction bestDirectionToNearestCoin;
            if (nearestCoin == CellLocation.TopLeft)
            {
                // No more coins are available,  game is meant to be finished.  Pick any direction
                // and hope to stay away from the ghosts
                bestDirectionToNearestCoin = currentLocation.AvailableMoves.First().Direction;
            }
            else
            {
                bestDirectionToNearestCoin = shortestDistances.DirectionToTarget(nearestCoin);
            }

            var nearestGhost = FindNearestGhost(_game.Ghosts, shortestDistances);

            bestDirectionToNearestCoin = AvoidGhost(currentLocation, shortestDistances, bestDirectionToNearestCoin, nearestGhost);

            return bestDirectionToNearestCoin;
        }

        private Direction AvoidGhost(LinkedCell currentLocation,
                                     Distances shortestDistances, Direction bestDirectionToNearestCoin, BotGhost? nearestGhost)
        {
            if (nearestGhost is object)
            {
                var distanceToGhost = shortestDistances.DistanceTo(nearestGhost.Location);
                if (distanceToGhost < 5)
                {
                    var directionToScaryGhost = shortestDistances.DirectionToTarget(nearestGhost.Location);
                    if (directionToScaryGhost == bestDirectionToNearestCoin)
                    {
                        bestDirectionToNearestCoin = PickDifferentDirection(currentLocation, directionToScaryGhost);
                    }
                }
            }

            return bestDirectionToNearestCoin;
        }

        private Direction PickDifferentDirection(LinkedCell currentLocation, Direction directionToScaryGhost)
        {
            var possibleDirections = currentLocation.AvailableMoves.ToList();
            var directionWeDontWantToTake = possibleDirections.Single(d => d.Direction == directionToScaryGhost);
            possibleDirections.Remove(directionWeDontWantToTake);

            return possibleDirections.First().Direction;
        }

        private CellLocation FindNearestCoin(IReadOnlyCollection<CellLocation> coins, Distances shortestDistances)
        {
            var bestCoin = default(CellLocation);
            var shortestDistance = int.MaxValue;

            foreach (var coin in coins)
            {
                var distanceToCoin = shortestDistances.DistanceTo(coin);

                if (distanceToCoin < shortestDistance)
                {
                    shortestDistance = distanceToCoin;
                    bestCoin = coin;
                }
            }

            return bestCoin;
        }

        private BotGhost? FindNearestGhost(BotGhost[] ghosts, Distances shortestDistances)
        {
            var closestGhost = default(BotGhost);
            var shortestDistance = int.MaxValue;

            foreach (var ghost in ghosts.Where(g => !g.Edible))
            {
                var distanceToGhost = shortestDistances.DistanceTo(ghost.Location);

                if (distanceToGhost < shortestDistance)
                {
                    shortestDistance = distanceToGhost;
                    closestGhost = ghost;
                }
            }

            return closestGhost;
        }
    }
}
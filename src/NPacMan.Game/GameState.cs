﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NPacMan.Game
{
    internal class GameState : IReadOnlyGameState
    {
        public GameState(IGameSettings settings)
        {
            Lives = settings.InitialLives;
            Status = settings.InitialGameStatus switch
            {
                GameStatus.Initial => nameof(GameStateMachine.Initial),
                GameStatus.Alive => nameof(GameStateMachine.GhostChase),
                GameStatus.Dying => nameof(GameStateMachine.Dying),
                GameStatus.Respawning => nameof(GameStateMachine.Respawning),
                GameStatus.Dead => nameof(GameStateMachine.Dead),
                _ => throw new NotImplementedException($"No map for InitialGameStatus '{settings.InitialGameStatus}")
            };
            Score = 0;
            GhostsVisible = true;
            TimeToChangeState = null;
            RemainingCoins = new List<CellLocation>(settings.Coins);
            RemainingPowerPills = new List<CellLocation>(settings.PowerPills);
            PacMan = settings.PacMan;
            Ghosts = settings.Ghosts.ToDictionary(x => x.Name, x => x);
        }

        public string Status { get; set; } = null!;

        public DateTime? TimeToChangeState { get;private set; }

        public int Lives { get; private set; }

        public bool GhostsVisible { get; private set; }

        public int Score { get; private set; }

        public DateTime LastTick { get; private set; }

        public IReadOnlyCollection<CellLocation> RemainingCoins { get; private set; }

        public IReadOnlyCollection<CellLocation> RemainingPowerPills { get; private set; }

        public IReadOnlyDictionary<string, Ghost> Ghosts { get; private set; }
        
        public PacMan PacMan { get; set; }

        internal void RemoveCoin(CellLocation location)
        {
            // Note - this is not the same as gameState.RemainingCoins = gameState.RemainingCoins.Remove(location)
            // We have to allow for the UI to be iterating over the list whilst we are removing elements from it.
            RemainingCoins = RemainingCoins.Where(c => c != location).ToList();
        }

        internal void RemovePowerPill(CellLocation location)
        {
            // Note - this is not the same as gameState.RemainingPowerPills = gameState.RemainingPowerPills.Remove(location)
            // We have to allow for the UI to be iterating over the list whilst we are removing elements from it.
            RemainingPowerPills = RemainingPowerPills.Where(p => p != location).ToList();
        }

        internal void MovePacManTo(CellLocation newPacManLocation)
        {
            PacMan = PacMan.WithNewLocation(newPacManLocation);
        }

        internal void IncreaseScore(int amount)
        {
            Score += amount;
        }

        internal void DecreaseLives()
        {
            Lives--;
        }

        internal void ShowGhosts()
        {
            GhostsVisible = true;
        }

        internal void HideGhosts()
        {
            GhostsVisible = false;
        }

        internal void RecordLastTick(DateTime now)
        {
            LastTick = now;
        }

        internal void ChangeStateIn(int timeInSeconds)
        {
            TimeToChangeState = LastTick.AddSeconds(timeInSeconds);
        }

        internal void ApplyToGhosts(Func<Ghost, Ghost> action)
        {
            var newPositionOfGhosts = new Dictionary<string, Ghost>();
            foreach (var ghost in Ghosts.Values)
            {
                newPositionOfGhosts[ghost.Name] = action(ghost);
            }

            Ghosts = newPositionOfGhosts;
        }

        internal void MovePacManHome()
        {
            PacMan = PacMan.SetToHome();
        }
    }
}
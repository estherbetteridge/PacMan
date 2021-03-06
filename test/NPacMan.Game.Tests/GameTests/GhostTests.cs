﻿using System;
using System.Dynamic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using GreenPipes;
using NPacMan.Game.GhostStrategies;
using NPacMan.Game.Tests.Helpers;
using Xunit;
using static NPacMan.Game.Tests.Helpers.Ensure;

namespace NPacMan.Game.Tests.GameTests
{
    public class GhostTests
    {
        private readonly TestGameSettings _gameSettings;
        private readonly TestGameClock _gameClock;

        public GhostTests()
        {
            _gameSettings = new TestGameSettings
            {
                PowerPills = { new CellLocation(50, 50) }
            };
            _gameClock = new TestGameClock();
        }

        [Fact]
        public async Task GhostMovesInDirectionOfStrategy()
        {
            var ghost = GhostBuilder.New()
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            await _gameClock.Tick();
            game.Ghosts[ghost.Name].Should().BeEquivalentTo(new
            {
                Location = new
                {
                    X = 1,
                    Y = 0
                }
            });
        }


        [Fact]
        public async Task GhostShouldNotMoveWhenPacManIsDying()
        {
            var x = 1;
            var y = 1;
            _gameSettings.InitialGameStatus = GameStatus.Dying;
            var ghost = GhostBuilder.New()
                    .WithLocation((x, y))
                    .WithChaseStrategy(new DirectToStrategy(new DirectToPacManLocation()))
                    .Create();
            _gameSettings.Ghosts.Add(ghost);
            _gameSettings.PacMan = new PacMan((3, 3), Direction.Down);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            await _gameClock.Tick();

            using var _ = new AssertionScope();
            game.Ghosts.Values.First()
                .Should().BeEquivalentTo(new
                {
                    Location = new
                    {
                        X = x,
                        Y = y
                    }
                });
        }



        [Fact]
        public async Task GhostShouldBeHiddenWhenPacManIsReSpawning()
        {
            _gameSettings.PacMan = new PacMan((3, 2), Direction.Left);
            var homeLocation = new CellLocation(1, 2);
            var ghost = GhostBuilder.New()
                .WithLocation(homeLocation)
                .WithDirection(Direction.Right)
                .WithChaseStrategy(new DirectToStrategy(new DirectToPacManLocation()))
                .Create();

            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            var now = DateTime.UtcNow;

            await _gameClock.Tick(now);
            await _gameClock.Tick(now.AddSeconds(4));

            game.Ghosts.Should().BeEmpty();
        }

        [Fact]
        public async Task GhostShouldBeBackAtHomeAfterPacManDiesAndComesBackToLife()
        {
            _gameSettings.PacMan = new PacMan((3, 2), Direction.Left);
            var homeLocation = new CellLocation(1, 2);
            var ghost = GhostBuilder.New()
                .WithLocation(homeLocation)
                .WithDirection(Direction.Right)
                .WithChaseStrategyRight()
                .Create();

            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            var now = DateTime.UtcNow;

            await _gameClock.Tick(now);
            await _gameClock.Tick(now.AddSeconds(4));

            if (game.Status != GameStatus.Respawning)
                throw new Exception($"Invalid Game State {game.Status:G} Should be Respawning");

            await _gameClock.Tick(now.AddSeconds(8));

            if (game.Status != GameStatus.Alive)
                throw new Exception($"Invalid Game State {game.Status:G} Should be Alive");

            game.Ghosts.Values.First()
                .Should().BeEquivalentTo(new
                {
                    Location = new
                    {
                        X = homeLocation.X,
                        Y = homeLocation.Y
                    }
                });
        }

        [Fact]
        public async Task GhostShouldScatterToStartWith()
        {
            _gameSettings.InitialGameStatus = GameStatus.Initial;
            _gameSettings.PacMan = new PacMan((10, 10), Direction.Left);
            var startingLocation = new CellLocation(3, 1);
            var scatterLocation = new CellLocation(1, 1);

            var ghost = GhostBuilder.New()
                .WithLocation(startingLocation)
                .WithScatterTarget(scatterLocation)
                .WithDirection(Direction.Right)
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            await _gameClock.Tick();
            await game.PressStart();
            await _gameClock.Tick();
            await _gameClock.Tick();

            game.Ghosts.Values.First()
                .Should().BeEquivalentTo(new
                {
                    Location = new
                    {
                        X = scatterLocation.X,
                        Y = scatterLocation.Y
                    }
                });
        }

        [Fact]
        public async Task GhostShouldChaseAfter7Seconds()
        {
            _gameSettings.InitialGameStatus = GameStatus.AttractMode;
            _gameSettings.PacMan = new PacMan((29, 10), Direction.Left);
            var startingLocation = new CellLocation(30, 1);
            var scatterLocation = new CellLocation(1, 1);

            var ghost = GhostBuilder.New()
                .WithLocation(startingLocation)
                .WithScatterTarget(scatterLocation)
                .WithDirection(Direction.Right)
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);
            await game.PressStart();

            await _gameClock.Tick(now);

            if (game.Ghosts.Values.First().Location.X != 29 || game.Ghosts.Values.First().Location.Y != 1)
                throw new System.Exception($"Ghost should be at 29,1 not {game.Ghosts.Values.First().Location.X}, {game.Ghosts.Values.First().Location.Y}");

            await _gameClock.Tick(now.AddSeconds(_gameSettings.InitialScatterTimeInSeconds + 1));
            await _gameClock.Tick(now.AddSeconds(_gameSettings.InitialScatterTimeInSeconds + 2));

            game.Ghosts.Values.First()
                .Should().BeEquivalentTo(new
                {
                    Location = new
                    {
                        X = 31,
                        Y = scatterLocation.Y
                    }
                });
        }

        [Fact]
        public async Task GhostShouldScatter7SecondsAfterChase()
        {
            _gameSettings.InitialGameStatus = GameStatus.AttractMode;
            _gameSettings.PacMan = new PacMan((29, 10), Direction.Left);
            var startingLocation = new CellLocation(30, 1);
            var scatterLocation = new CellLocation(1, 1);

            var ghost = GhostBuilder.New()
                .WithLocation(startingLocation)
                .WithScatterTarget(scatterLocation)
                .WithDirection(Direction.Right)
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);
            await game.PressStart();
            WeExpectThat(ourGhost()).IsAt(startingLocation);
            await _gameClock.Tick(now);
            WeExpectThat(ourGhost()).IsAt(startingLocation.Left);

            await _gameClock.Tick(now.AddSeconds(_gameSettings.InitialScatterTimeInSeconds + 1));
            WeExpectThat(ourGhost()).IsAt(startingLocation.Left.Right);

            await _gameClock.Tick(now.AddSeconds(_gameSettings.InitialScatterTimeInSeconds + 2));
            WeExpectThat(ourGhost()).IsAt(startingLocation.Left.Right.Right);

            now = now.AddSeconds(_gameSettings.InitialScatterTimeInSeconds);
            await _gameClock.Tick(now.AddSeconds(_gameSettings.ChaseTimeInSeconds + 1));
            WeExpectThat(ourGhost()).IsAt(startingLocation.Left.Right.Right.Left);

            await _gameClock.Tick(now.AddSeconds(_gameSettings.ChaseTimeInSeconds + 2));

            ourGhost()
                .Should().BeEquivalentTo(new
                {
                    Location = new
                    {
                        X = 29,
                        Y = 1
                    }
                });


            Ghost ourGhost() => game.Ghosts.Values.First();
        }

        [Fact]
        public async Task GhostsDoNotStartOffAsEdible()
        {

            var ghosts = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.Right.Right)
                .CreateMany(3);
            _gameSettings.Ghosts.AddRange(ghosts);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await _gameClock.Tick();

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Edible = false
            });
        }


        [Theory]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(10)]
        public async Task AllGhostsShouldReturnToNonEdibleAfterGivenAmountOfSeconds(int seconds)
        {
            _gameSettings.FrightenedTimeInSeconds = seconds;
            var ghosts = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .CreateMany(3);
            _gameSettings.Ghosts.AddRange(ghosts);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Right);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);

            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            await _gameClock.Tick(now.AddSeconds(seconds).AddMilliseconds(-100));
            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            await _gameClock.Tick(now.AddSeconds(seconds));

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Edible = false
            });
        }

        [Theory]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(10)]
        public async Task GhostShouldReturnToNonEdibleAfterGivenAmountSecondsWhenEatingSecondPowerPills(int seconds)
        {
            _gameSettings.FrightenedTimeInSeconds = seconds;
            var ghosts = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .CreateMany(3);
            _gameSettings.Ghosts.AddRange(ghosts);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right);
            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right.Right.Right);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Right);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);
            await _gameClock.Tick(now.AddSeconds(seconds - 1));
            await _gameClock.Tick(now.AddSeconds(seconds - 1));

            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            await _gameClock.Tick(now.AddSeconds(seconds * 2 - 1).AddMilliseconds(-100));
            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            await _gameClock.Tick(now.AddSeconds(seconds * 2 - 1));

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Edible = false
            });
        }

        [Fact]
        public async Task AllGhostsShouldRemainInLocationAfterPowerPillIsEaten()
        {
            var ghostStart = _gameSettings.PacMan.Location.Below.Right;
            var ghosts = GhostBuilder.New()
                .WithLocation(ghostStart)
                .CreateMany(3);
            _gameSettings.Ghosts.AddRange(ghosts);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Right);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);

            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Location = ghostStart
            });
        }


        [Fact]
        public async Task AllGhostsShouldShouldStayInSameLocationWhenTransitioningToNoneEdible()
        {
            var ghostStart = _gameSettings.PacMan.Location.Below.Right;
            var ghosts = GhostBuilder.New()
                .WithLocation(ghostStart)
                .WithChaseStrategyRight()
                .CreateMany(3);

            _gameSettings.Ghosts.AddRange(ghosts);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Right);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);

            if (!game.Ghosts.Values.All(g => g.Edible))
                throw new Exception("All ghosts are meant to be edible.");

            await _gameClock.Tick(now.AddSeconds(7));

            if (!game.Ghosts.Values.All(g => !g.Edible))
                throw new Exception("All ghosts are meant to be nonedible.");

            using var _ = new AssertionScope();

            foreach (var ghost in game.Ghosts.Values)
            {
                ghost.Should().NotBeEquivalentTo(new
                {
                    Location = ghostStart
                });
            }
        }

        [Fact]
        public async Task TheEdibleGhostBecomesNonEdibleAfterBeingEaten()
        {
            var ghostStart1 = _gameSettings.PacMan.Location.Left.Left.Left;
            var ghostStart2 = _gameSettings.PacMan.Location.Below.Below.Below;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(ghostStart2)
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Left);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);

            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left);
            WeExpectThat(game.Ghosts[ghost1.Name]).IsAt(ghostStart1.Right);

            await _gameClock.Tick(now);
            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left.Left);


            using var _ = new AssertionScope();
            game.Ghosts[ghost1.Name].Should().BeEquivalentTo(new
            {
                Edible = false
            });

            game.Ghosts[ghost2.Name].Should().BeEquivalentTo(new
            {
                Edible = true
            });

        }

        [Fact]
        public async Task TheEdibleGhostMovesAtHalfSpeedWhileFrightened()
        {
            var ghostStart1 = _gameSettings.PacMan.Location.Below.Below;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost1);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Left);

            await _gameClock.Tick();

            await _gameClock.Tick();
            await _gameClock.Tick();
            await _gameClock.Tick();
            await _gameClock.Tick();

            using var _ = new AssertionScope();
            game.Ghosts[ghost1.Name].Should().BeEquivalentTo(new
            {
                Location = ghostStart1.Right.Right.Right
            });
        }

        [Fact]
        public async Task GhostMovesRandomlyWhileFrightened()
        {
            var testDirectionPicker = new TestDirectionPicker();
            testDirectionPicker.DefaultDirection = Direction.Up;
            var allDirections = new[] { Direction.Down, Direction.Up, Direction.Right };
            testDirectionPicker.Returns(allDirections, Direction.Right);

            _gameSettings.DirectionPicker = testDirectionPicker;
            var ghostStart1 = _gameSettings.PacMan.Location.FarAway();
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .Create();
            _gameSettings.Ghosts.Add(ghost1);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            // PacMan Eat Pill
            await game.ChangeDirection(Direction.Left);
            await _gameClock.Tick();

            await _gameClock.Tick();
            await _gameClock.Tick();

            await _gameClock.Tick();
            await _gameClock.Tick();

            using var _ = new AssertionScope();
            game.Ghosts[ghost1.Name].Should().BeEquivalentTo(new
            {
                Location = ghostStart1.Right.Right
            });
        }


        [Fact]
        public async Task GhostIsTeleportedWhenWalkingIntoAPortal()
        {
            var portalEntrance = new CellLocation(10, 1);
            var portalExit = new CellLocation(1, 5);
            var ghost1 = GhostBuilder.New()
                .WithLocation(portalEntrance.Left)
                .WithChaseStrategyRight()
                .Create();

            _gameSettings.Ghosts.Add(ghost1);
            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            _gameSettings.Portals.Add(portalEntrance, portalExit);

            await _gameClock.Tick();

            game.Ghosts.Values.First()
                .Should()
                .BeEquivalentTo(new
                {
                    Location = portalExit.Right
                });
        }


        [Fact]
        public async Task GhostsShouldStandStillInGhostHouse()
        {
            var ghostStart1 = _gameSettings.PacMan.Location.FarAway();
            _gameSettings.GhostHouse.Add(ghostStart1);
            var ghosts = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithNumberOfCoinsRequiredToExitHouse(int.MaxValue)
                .CreateMany(2);
            _gameSettings.Ghosts.AddRange(ghosts);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await _gameClock.Tick();
            await _gameClock.Tick();

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Location = ghostStart1
            });
        }

        [Fact]
        public async Task GhostsShouldLeaveGhostHouseWhenPillCountHasBeenReached()
        {
            // X
            // -
            // H G

            var ghostStart1 = new CellLocation(1, 2);
            _gameSettings.PacMan = new PacMan(ghostStart1.FarAway(), Direction.Left);
            _gameSettings.GhostHouse.AddRange(new[] { ghostStart1, ghostStart1.Left });
            _gameSettings.Doors.Add(ghostStart1.Left.Above);
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithNumberOfCoinsRequiredToExitHouse(1)
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithNumberOfCoinsRequiredToExitHouse(10)
                .Create();
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);

            _gameSettings.Coins.Add(_gameSettings.PacMan.Location.FarAway());
            _gameSettings.Coins.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();
            await game.ChangeDirection(Direction.Left);

            // PacMan Eats Coin
            await _gameClock.Tick();

            // Still in House, under door
            await _gameClock.Tick();

            // On ghost door
            await _gameClock.Tick();

            // Out of house
            await _gameClock.Tick();

            using var _ = new AssertionScope();
            game.Ghosts[ghost1.Name].Should().BeEquivalentTo(new
            {
                Location = ghostStart1.Left.Above.Above
            });
            game.Ghosts[ghost2.Name].Should().BeEquivalentTo(new
            {
                Location = ghostStart1
            });
        }

        [Fact]
        public async Task GhostsShouldBeNotEdibleAfterPacManComesBackToLife()
        {
            //     G
            // > .   #       G
            //     -
            //     H
            var ghost1 = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.Right.Right.Above)
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .Create();
            var directionPicker = new TestDirectionPicker() { DefaultDirection = Direction.Down };
            _gameSettings.DirectionPicker = directionPicker;
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);
            _gameSettings.Walls.Add(_gameSettings.PacMan.Location.Right.Right.Right);
            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Right);
            _gameSettings.Doors.Add(ghost1.Location.Below.Below);
            _gameSettings.GhostHouse.Add(_gameSettings.Doors.First().Below);

            var now = DateTime.UtcNow;
            var gameHarness = new GameHarness(_gameSettings);
            gameHarness.StartGame();
            await gameHarness.ChangeDirection(Direction.Right);

            await gameHarness.EatPill();

            gameHarness.WeExpectThatGhost(ghost1).IsEdible();
            gameHarness.WeExpectThatGhost(ghost2).IsEdible();

            await gameHarness.EatGhost(ghost1);

            gameHarness.WeExpectThatGhost(ghost1).IsNotEdible();
            gameHarness.WeExpectThatGhost(ghost2).IsEdible();

            await gameHarness.WaitForPauseToComplete();

            // Move to Ghost House
            await gameHarness.Move();
            await gameHarness.Move();

            await gameHarness.Move();
            await gameHarness.GetEatenByGhost(ghost1);
         
            gameHarness.EnsureGameStatus(GameStatus.Dying);

            await gameHarness.WaitToFinishDying();
 
            gameHarness.EnsureGameStatus(GameStatus.Respawning);

            await gameHarness.WaitToRespawn();

            gameHarness.EnsureGameStatus(GameStatus.Alive);

            gameHarness.Game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Edible = false
            });
        }

        [Fact]
        public async Task EatenGhostNotificationShouldBeFiredAfterEatingAGhost()
        {
            var ghostStart = _gameSettings.PacMan.Location.Left.Left.Left;
            var ghost1 = GhostBuilder.New().WithLocation(ghostStart).WithChaseStrategyRight()
                .Create();

            _gameSettings.Ghosts.Add(ghost1);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            var numberOfNotificationsTriggered = 0;
            game.Subscribe(GameNotification.EatGhost, () => numberOfNotificationsTriggered++);
            game.StartGame();

            await game.ChangeDirection(Direction.Left);

            await _gameClock.Tick();

            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left);

            WeExpectThat(game.Ghosts.Values).IsAt(ghostStart.Right);

            if (numberOfNotificationsTriggered != 0)
            {
                throw new Exception($"No EatGhost notifications should have been fired but {numberOfNotificationsTriggered} were.");
            }

            await _gameClock.Tick();

            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left.Left);

            numberOfNotificationsTriggered.Should().Be(1);
        }


        [Fact]
        public async Task GhostsShouldBeInScatterModeAfterPacManComesBackToLife()
        {
            //   G         
            // > .       
            var ghost = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.Right.Above)
                .WithScatterTarget(_gameSettings.PacMan.Location.Right.Above.Above)
                .Create();

            var directionPicker = new TestDirectionPicker() { DefaultDirection = Direction.Down };
            _gameSettings.DirectionPicker = directionPicker;
            _gameSettings.Ghosts.Add(ghost);

            var now = DateTime.UtcNow;
            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Right);
            await _gameClock.Tick(now);

            await game.ChangeDirection(Direction.Up);
            await _gameClock.Tick(now);   //Now dead

            if (game.Status != GameStatus.Dying)
                throw new Exception($"Invalid Game State {game.Status:G} Should be Dying");

            await _gameClock.Tick(now.AddSeconds(4));

            if (game.Status != GameStatus.Respawning)
                throw new Exception($"Invalid Game State {game.Status:G} Should be Respawning");

            await _gameClock.Tick(now.AddSeconds(8));

            if (game.Status != GameStatus.Alive)
                throw new Exception($"Invalid Game State {game.Status:G} Should be Alive");

            await _gameClock.Tick(now.AddSeconds(9));

            game.Ghosts.Values.Should().AllBeEquivalentTo(new
            {
                Edible = false,
                Location = _gameSettings.PacMan.Location.Right.Above.Above
            });
        }


        [Fact]
        public async Task TheGamePausesAfterGhostIsEaten()
        {
            var ghostStart1 = _gameSettings.PacMan.Location.Left.Left.Left;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Left);

            var game = new Game(_gameClock, _gameSettings);
            game.StartGame();

            await game.ChangeDirection(Direction.Left);

            var now = DateTime.UtcNow;
            await _gameClock.Tick(now);

            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left);
            WeExpectThat(game.Ghosts[ghost1.Name]).IsAt(ghostStart1.Right);

            await _gameClock.Tick(now);
            WeExpectThat(game.PacMan).IsAt(_gameSettings.PacMan.Location.Left.Left);

            var pacManLocation = game.PacMan.Location;
            var ghostLocations = game.Ghosts.Values.Select(x => new {
                x.Name,
                x.Location,
                Status = x.Name == ghost1.Name ? GhostStatus.Score : GhostStatus.Edible
            }).ToDictionary(x => x.Name);

            // Should not move
            await _gameClock.Tick(now);
            await _gameClock.Tick(now);
            await _gameClock.Tick(now);

            using var _ = new AssertionScope();
            game.Should().BeEquivalentTo(new
            {
                PacMan = new
                {
                    Location = pacManLocation
                },
                Ghosts = ghostLocations
            });
        }

        [Fact]
        public async Task TheGameResumesAfterBeingPausedForOneSecondAfterGhostIsEaten()
        {
            //      P
            //   .  *
            // G .  .

            var ghostStart1 = _gameSettings.PacMan.Location.Below.Below.Left.Left;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Below);


            var gameHarness = new GameHarness(_gameSettings);
            gameHarness.Game.StartGame();

            await gameHarness.ChangeDirection(Direction.Down);
            await gameHarness.EatPill();

            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below);
            gameHarness.WeExpectThatGhost(ghost1).IsAt(ghostStart1.Right);

            await gameHarness.EatGhost(ghost1);
            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below.Below);

            var pacManLocation = gameHarness.Game.PacMan.Location;
            var ghostLocations = gameHarness.Game.Ghosts.Values.Select(x => new {
                x.Name,
                x.Location
            }).ToDictionary(x => x.Name);

            await gameHarness.WaitForPauseToComplete();
            await gameHarness.Move();

            using var _ = new AssertionScope();
            gameHarness.Game.PacMan.Should().NotBeEquivalentTo(new
                {
                    Location = pacManLocation
                });
            gameHarness.Game.Ghosts.Should().NotBeEmpty();
            gameHarness.Game.Ghosts.Should().NotBeEquivalentTo(ghostLocations);
        }

        [Fact]
        public async Task GhostShouldBeRunningHomeAfterThePauseAfterBeingTheEaten()
        {
            //      P
            //   .  *
            // G .  .

            var ghostStart1 = _gameSettings.PacMan.Location.Below.Below.Left.Left;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();
            var ghost2 = GhostBuilder.New()
                .WithLocation(_gameSettings.PacMan.Location.FarAway())
                .WithChaseStrategyRight()
                .Create();
            _gameSettings.Ghosts.Add(ghost1);
            _gameSettings.Ghosts.Add(ghost2);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Below);


            var gameHarness = new GameHarness(_gameSettings);
            gameHarness.Game.StartGame();

            await gameHarness.ChangeDirection(Direction.Down);
            await gameHarness.EatPill();

            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below);
            gameHarness.WeExpectThatGhost(ghost1).IsAt(ghostStart1.Right);

            await gameHarness.EatGhost(ghost1);
            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below.Below);

            var pacManLocation = gameHarness.Game.PacMan.Location;
            var ghostLocations = gameHarness.Game.Ghosts.Values.Select(x => new {
                x.Name,
                x.Location
            }).ToDictionary(x => x.Name);

            await gameHarness.WaitForPauseToComplete();
            await gameHarness.Move();

            using var _ = new AssertionScope();
            gameHarness.Game.Ghosts[ghost1.Name].Status.Should().Be(GhostStatus.RunningHome);
            gameHarness.Game.Ghosts[ghost2.Name].Status.Should().Be(GhostStatus.Edible);
        }

        [Fact]
        public async Task GhostShouldBeInTheHouseAfterBeingEatingAndWaitingForElapsedTime()
        {
            //      P     
            //   .  *   # - - #
            // G .  .   # H H #
            //          # # # #
            var ghostStart1 = _gameSettings.PacMan.Location.Below.Below.Left.Left;
            var ghost1 = GhostBuilder.New()
                .WithLocation(ghostStart1)
                .WithChaseStrategyRight()
                .Create();

            var topLeftWall = _gameSettings.PacMan.Location.Right.Right.Below;
            _gameSettings.Walls.Add(topLeftWall);
            _gameSettings.Walls.Add(topLeftWall.Below);
            _gameSettings.Walls.Add(topLeftWall.Below.Below);
            _gameSettings.Walls.Add(topLeftWall.Below.Below.Right);
            _gameSettings.Walls.Add(topLeftWall.Below.Below.Right.Right);
            _gameSettings.Walls.Add(topLeftWall.Below.Below.Right.Right.Right);
            _gameSettings.Walls.Add(topLeftWall.Below.Right.Right.Right);
            _gameSettings.Walls.Add(topLeftWall.Right.Right.Right);

            _gameSettings.Doors.Add(topLeftWall.Right);
            _gameSettings.Doors.Add(topLeftWall.Right.Right);

            var ghostHouse = topLeftWall.Below.Right;
            _gameSettings.GhostHouse.Add(ghostHouse);
            _gameSettings.GhostHouse.Add(topLeftWall.Below.Right.Right);
            
            _gameSettings.Ghosts.Add(ghost1);

            _gameSettings.PowerPills.Add(_gameSettings.PacMan.Location.Below);


            var gameHarness = new GameHarness(_gameSettings);
            gameHarness.Game.StartGame();

            await gameHarness.ChangeDirection(Direction.Down);
            await gameHarness.EatPill();

            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below);
            gameHarness.WeExpectThatGhost(ghost1).IsAt(ghostStart1.Right);

            await gameHarness.EatGhost(ghost1);
            gameHarness.WeExpectThatPacMan().IsAt(_gameSettings.PacMan.Location.Below.Below);

            await gameHarness.WaitForPauseToComplete();
            await gameHarness.Move();
            await gameHarness.Move();
            await gameHarness.Move();
            await gameHarness.Move();
            await gameHarness.Move();
            await gameHarness.Move();
            gameHarness.WeExpectThatGhost(ghost1).IsAt(topLeftWall.Right);
            await gameHarness.Move();

            using var _ = new AssertionScope();
            gameHarness.Game.Ghosts[ghost1.Name].Status.Should().Be(GhostStatus.Alive);
            gameHarness.Game.Ghosts[ghost1.Name].Location.Should().Be(ghostHouse);
        }
    }
}

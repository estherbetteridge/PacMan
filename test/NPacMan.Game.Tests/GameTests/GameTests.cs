using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using static NPacMan.Game.Tests.GameTests.Ensure;

namespace NPacMan.Game.Tests.GameTests
{
    public class GameTests
    {
        private readonly TestGameSettings _gameSettings;
        private readonly TestGameClock _gameClock;
        private readonly Game _game;

        public GameTests()
        {
            _gameSettings = new TestGameSettings();
            _gameClock = new TestGameClock();
            _game = new Game(_gameClock, _gameSettings);
        }

        [Fact]
        public void GameStartsWithThreeLives()
        {
            _game.Lives.Should().Be(3);
        }

        [Fact]
        public void TheGameCanReadTheWidthFromTheBoard()
        {
            var gameBoardWidth = 100;
            _gameSettings.Width = gameBoardWidth;

            _game.Width.Should().Be(gameBoardWidth);
        }

        [Fact]
        public void TheGameCanReadTheHeightFromTheBoard()
        {
            var gameBoardHeight = 100;
            _gameSettings.Height = gameBoardHeight;

            _game.Height.Should().Be(gameBoardHeight);
        }

        [Fact]
        public void CanReadPortalsFromGame()
        {
            _gameSettings.Portals.Add((1,2),(3,4));

            _game.Portals.Should().BeEquivalentTo(new Dictionary<CellLocation, CellLocation> {
                [(1,2)] = (3,4)
            });
        }

        [Fact]
        public void DoorsShouldAlsoBeAGameWall()
        {
            _gameSettings.Doors.Add((1,1));
            _gameSettings.Doors.Add((1,2));
            _gameSettings.Walls.Add((1,3));
            _gameSettings.Walls.Add((1,4));

            var game = new Game(_gameClock, _gameSettings);

            game.Walls.Should().BeEquivalentTo(new CellLocation[] {
                (1,1),
                (1,2),
                (1,3),
                (1,4)
            });
        }

        [Fact]
        public async Task BeforeATickIfProcessedTheGameNotificationShouldFire()
        {
            var numberOfNotificationsTriggered = 0;

            var gameClock = new TestGameClock();
            var game = new Game(gameClock, _gameSettings);
            game.Subscribe(GameNotification.PreTick, () => numberOfNotificationsTriggered++);
            game.StartGame();

            using var _ = new AssertionScope();

            numberOfNotificationsTriggered.Should().Be(0);

            await gameClock.Tick();
            numberOfNotificationsTriggered.Should().Be(1);

            await gameClock.Tick();
            numberOfNotificationsTriggered.Should().Be(2);

            await gameClock.Tick();
            numberOfNotificationsTriggered.Should().Be(3);
        }

    }
}

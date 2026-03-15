using Microsoft.Extensions.DependencyInjection;
using DungeonOfShadows.Core;

var config = new GameConfig();
using var services = ServiceRegistration.Build(config);
var game = services.GetRequiredService<Game>();
game.Run();

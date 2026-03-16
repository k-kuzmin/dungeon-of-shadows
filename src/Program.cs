using Microsoft.Extensions.DependencyInjection;
using DungeonOfShadows.Core;

var config = new GameConfig();
var services = ServiceRegistration.Build(config);
var game = services.GetRequiredService<Game>();
game.Run();

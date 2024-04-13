const bool isServer = true;  // TODO: Change to false for client

PolyNetworking.Networking.StartNetworking(isServer);

using var game = new polyframework.MinigameExampleTwoCars();
game.Run(isServer);

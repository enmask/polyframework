//const bool isServer = true;  // TODO: Change to false for client
const int clientNo = 0;  // TODO: Change to 1, 2, 3 for clients
const bool isServer = clientNo == 0;

PolyNetworking.Networking.StartNetworking(isServer);

using var game = new polyframework.MinigameExampleTwoCars();
//using var6 game = new polyframework.CarRace();
// 0 for server, 1 for client 1, 2 for client 2 etc.
game.Run(clientNo);

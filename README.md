# Online Tic-Tac-Toe (Client + Server)

Naz Yagmur · Course 3130 · George Brown College

A client-server Tic-Tac-Toe game built with Unity 6 and Unity Transport. It has accounts, game rooms, online PvP that the server controls, in-game chat (a message wheel and free text), observers, and a local two-player mode.

| Project | Repository |
|---|---|
| Server | https://github.com/nazyg/TicTacToeServer |
| Client | https://github.com/nazyg/TicTacToeClient |

## Requirements

- Unity **6000.6.0f1** (another Unity 6 version should also work; Unity will offer to upgrade the project)
- Unity Transport (already listed in `Packages/manifest.json`, so Unity installs it automatically)

## How to run

The server and the client are two separate Unity projects. Unity can't open the same project twice, so the easiest way to test is to build the client and run it as an `.exe`.

1. **Build the client once**
   - Open **TicTacToeClient** in Unity.
   - Go to *File → Build Profiles*, pick Windows (or Mac), and choose **Build**.
   - Close the client project.
2. **Start the server**
   - Open **TicTacToeServer** in Unity and press **Play**.
   - The Console shows where the accounts file is saved.
3. **Start two clients**
   - Run the client build twice.
   - Each window connects to `127.0.0.1` on port `9001`.
4. **Play**
   - In each window, create an account with *Create a new account*, then log in.
   - Both players type the same room name and press **Join / Create Game Room**.
   - To watch a game, a third client logs in, types the room name and presses **Watch Game Room**.

To run the server and clients on **different computers**, change `IPAddress` in `TicTacToeClient/Assets/Scenes/Scripts/NetworkClient.cs` to the server computer's local IP address, then build the client again.

## Features

- **Accounts:** create an account and log in. Every error has its own message, and passwords are saved as salted SHA-256 hashes.
- **Game rooms:** create or join a room by name, with a waiting screen and a Back button.
- **Online PvP:** the server owns the board and checks every move (whose turn it is, empty cell, game over). Play Again starts a rematch.
- **Chat:** six quick messages plus free text, shown in a chat log.
- **Observers:** watch a running game live without being able to make moves.
- **Local game:** two players take turns on one computer, opened from the lobby.

## Code structure

| File | Project | Responsibility |
|---|---|---|
| `NetworkServer.cs` / `NetworkClient.cs` | both | Unity Transport connections, sending and receiving |
| `NetworkServerProcessing.cs` | server | Reads incoming messages and passes them to the managers |
| `NetworkClientProcessing.cs` | client | Reads server messages and builds outgoing messages |
| `AccountManager.cs` | server | Accounts and which connection belongs to which account |
| `GameRoomManager.cs` | server | Rooms, players, observers, boards, chat |
| `TicTacToeBoard.cs` | both | Game rules (no Unity code) |
| `SharedNetworkDefinitions.cs` | both | Message signifiers, result codes and validation rules |
| `GameUIManager.cs`, `TicTacToeBoardView.cs`, `UIFactory.cs` | client | UI state machine and screens (built from code, so the scene needs no setup) |

Messages are comma-separated strings that start with a signifier, for example `7,4` means "place a mark on cell 4". The full list is in `SharedNetworkDefinitions.cs`.

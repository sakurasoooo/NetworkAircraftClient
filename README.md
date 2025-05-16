# NetworkAircraftClient

NetworkAircraftClient is a Unity-based game client for a multiplayer aircraft game. It connects to the `NetworkAircraftServer` to send player actions and receive game state updates, rendering the game world and interactions for the player.

## Features

* **Real-time Multiplayer Gameplay:** Connects to a dedicated server to participate in a shared game session.
* **Player Control:** Allows players to control their aircraft's movement and initiate attacks.
* **Dynamic Game World:** Renders other players, a boss entity, and projectiles (player rockets, boss rockets) based on server updates.
* **HUD:** Displays player health.
* **Input System:** Utilizes Unity's new Input System for player controls.
* **Networking:** Handles TCP communication with the game server, sending player inputs and receiving broadcasted game states.
* **On-Screen Debug Log:** Includes a utility for displaying debug messages directly on the game screen.

## Requirements

* **Unity Editor Version:** 2022.3.1f1
* **NetworkAircraftServer:** A running instance of the [NetworkAircraftServer](https://github.com/sakurasoooo/NetworkAircraftServer) (replace with the actual server repository link if available).

## Setup & Installation

1.  **Clone the repository (if you haven't already):**
    ```bash
    git clone https://github.com/sakurasoooo/NetworkAircraftClient.git
    cd NetworkAircraftClient-main
    ```
2.  **Open the project in Unity Hub:**
    * Open Unity Hub.
    * Click "Open" or "Add project from disk".
    * Navigate to the `NetworkAircraftClient-main` folder and select it.
    * Unity will open the project. It might take some time to import assets for the first time.
3.  **Configure Server Address (if needed):**
    * In the Unity Editor, locate the `NetworkManager` GameObject in your main scene (likely `SampleScene`).
    * In the Inspector window, find the `Network Manager` script component.
    * Ensure the `Server Address` (default: "localhost") and `Server Port` (default: 8080) are correctly set to point to your running `NetworkAircraftServer` instance.
4.  **Run the game:**
    * Press the Play button in the Unity Editor.

## Key Scripts and Functionality

* **`NetworkManager.cs` (Singleton):**
    * Manages the TCP connection to the `NetworkAircraftServer`.
    * Handles sending player actions (movement, attacks) to the server.
    * Receives and processes broadcasted game state updates from the server.
    * Instantiates and updates GameObjects (players, boss, rockets) in the scene based on server data.
    * Maintains dictionaries (`players`, `playerRockets`, `bosses`, `bossRockets`) to manage networked entities.
    * Defines data structures for communication with the server (`MoveDataRequest`, `NewPlayerData`, `BroadcastMessage`, `Position`).

* **`PlayerNetworkController.cs`:**
    * Attached to the player's controllable aircraft.
    * Uses Unity's `PlayerInput` component to read player actions (movement) defined in `PlayerInputMap.inputactions`.
    * Sends movement data to the `NetworkManager` at regular intervals.
    * Applies local movement for immediate visual feedback (though server state is authoritative).

* **`UIControl.cs`:**
    * Manages UI elements, specifically the "Shoot" button.
    * Calls `NetworkManager.SendAttackData()` when the shoot button is pressed, with a cooldown mechanism.

* **`PlayerData.cs`:**
    * A simple component attached to player-controlled and other networked entities.
    * Stores entity-specific data like `health` and `uuid` (assigned by the server).

* **`PlayerHUD.cs`:**
    * Updates the player's health bar UI element based on the `PlayerData` of the local player.

* **`PlayerRocketController.cs` & `BossRocketController.cs`:**
    * Likely attached to rocket prefabs. Their current implementation in the provided files is minimal. They might be intended for client-side effects or collision handling if not fully server-authoritative.

* **`SelfRotation.cs`:**
    * A utility script to make GameObjects rotate on their Z-axis at a random speed, likely used for visual flair on some objects (e.g., background elements or pickups if any).

* **`ScreenLog.cs`:**
    * Provides an on-screen display of Unity's debug console messages, useful for development and debugging directly in builds.

## Network Communication

* **Connection:** The client attempts to connect to the server address and port specified in the `NetworkManager`. It will retry a few times if the initial connection fails.
* **Initial Data:** Upon successful connection, the server sends initial data for the player (UUID, name, starting position). The client then instantiates the local player's prefab.
* **Sending Data:**
    * **Movement:** `PlayerNetworkController` reads input and periodically calls `NetworkManager.Instance.SendMoveData(uuid, moveVector)` to inform the server about the player's intended movement.
    * **Attacks:** `UIControl` calls `NetworkManager.Instance.SendAttackData()` when the player shoots. This sends an attack request including the player's UUID and the target's UUID (currently targets the main boss).
* **Receiving Data:**
    * The `NetworkManager` continuously listens for broadcast messages from the server in a coroutine.
    * Server messages are received as a JSON array of `BroadcastMessage` objects.
    * Each message contains the `type` (player, boss, playerRocket, bossRocket), `uuid`, `health`, `position`, etc.
    * The `NetworkManager.UpdateNetwork(message)` function processes these messages:
        * If an entity with the given UUID exists, its position and health are updated.
        * If an entity does not exist and its health is > 0, a new GameObject is instantiated using the appropriate prefab (e.g., `playerallyPrefab` for other players, `bossPrefab` for the boss, `playerRocketPrefab`, `bossRocketPrefab`).
        * If an entity's health is <= 0, its GameObject is destroyed.
        * Rockets are also rotated to face their direction of movement.

## Input Controls


* **Movement:** Likely uses a 2D Vector action (e.g., WASD keys, gamepad left stick) mapped to the "Move" action in `PlayerInputMap`.
* **Shoot:** Triggered by a UI button, which is handled by `UIControl.cs`.

## Key Prefabs

Located in the `Assets/Prefabs/` folder (assumption, standard Unity practice):

* `playerPrefab`: Represents the player's controllable aircraft.
* `playerRocketPrefab`: Represents rockets fired by players.
* `bossPrefab`: Represents the main boss enemy.
* `bossRocketPrefab`: Represents rockets fired by the boss.
* `playerallyPrefab`: Represents other connected players' aircraft.

## Unity Packages Used (from `manifest.json`)

* `com.unity.inputsystem`: For handling player input.
* `com.unity.textmeshpro`: For advanced text rendering (likely used in UI).
* `com.unity.adaptiveperformance`: For performance scaling on mobile devices (though the current target seems to be standalone).
* Standard 2D feature packages.
* And other common Unity modules.

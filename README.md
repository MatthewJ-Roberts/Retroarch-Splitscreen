# Retroarch Splitscreen

A screen splitter for Retroarch - load two instances of Retroarch seamlessly and simultaneously to play with a friend!

**WARNING:** This was made for Retroarch, but can technically launch ANY .exe. Only Retroarch's functionality has been tested, and development for other tools/games is not realistic.

## Features

1. Open multiple instances of Retroarch with views programmed for 2, 3, or 4 players.
2. If playing with 2 players, use your preference of either a horizontal or vertical split.

**Please note:** This application isn't as advanced as something like Nucleus Coop. It won't create local multiplayer sessions out of online-only games. It's just used as a means to open multiple instances of Retroarch and handle sizing and placement automatically.

## Use Cases

1. Local Pokemon Soul Links
2. Local speedrun competitions
3. Playing two different games side-by-side
4. Anything else you can think of!

## How to Use

1. Download the latest release.
2. Run `Retroarch Splitscreen.exe`.
3. Select the split direction.
4. Select the number of players.
5. Press "Browse" and select the location of your Retroarch executable:
   - Default path: `C:\RetroArch-Win64\retroarch.exe`
6. Press "Browse" and select the location of your Retroarch config file:
   - Default path: `C:\RetroArch-Win64\retroarch.cfg`
7. Set the Menu Scale Factor (0.2 - 5):
   - Description: How big the Retroarch menus will be. Does not impact gameplay, purely for menu navigation purposes. Will not overwrite the old value so no need to worry about having to changing it back
   - Default value: 0.5
8. Press "Launch Splitter".
9. Ensure you have at least 2 different controllers and configure them within the Retroarch instances:
   - Set a designated controller to "Port 1" in both instances
   - Set "Port 2" to "N/A" in both instances
10. Repeat step 9 for however many controllers you have, ensuring each Retroarch instance has their dedicated controller assigned to "Port 1".
11. Make sure that the following is disabled:
    - Path: `Settings --> User Interface --> Pause Content When Not Active --> Off`
12. Done!

## Examples Screenshots

### 2 Players: Horizontal
![Screenshot 2024-06-03 151535](https://github.com/MatthewJ-Roberts/Retroarch-Splitscreen/assets/63058876/fae57546-5bf8-44ea-b045-acb9df32d357)

### 2 Players: Vertical
![Screenshot 2024-06-03 152144](https://github.com/MatthewJ-Roberts/Retroarch-Splitscreen/assets/63058876/da38740a-bfe3-4bda-a509-e563cb6bf327)

### 3 Players
![Screenshot 2024-06-03 152239](https://github.com/MatthewJ-Roberts/Retroarch-Splitscreen/assets/63058876/0b9982d9-b415-4e75-9089-64a0cfc884a7)

### 4 Players
![Screenshot 2024-06-03 152323](https://github.com/MatthewJ-Roberts/Retroarch-Splitscreen/assets/63058876/eff5a23e-44da-44fe-a7f6-f2da4e446194)

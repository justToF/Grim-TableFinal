# Project Overview 
- Game Title: Grim Table
- High-Level Concept: Survival card game at a table with Grim as the dealer.
- Players: Single player + AI bots.
- Inspiration / Reference Games: Liar's Bar, Russian Roulette style games.
- Tone / Art Direction: Dark, atmospheric, stylized.
- Target Platform: PC (StandaloneWindows64)
- Screen Orientation / Resolution: Landscape
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics 
## Core Gameplay Loop
The player participates in rounds where they receive cards, play them to stay alive or gain advantages, and then Grim (the entity) selects an event and a target to execute it upon.

## Scripted Tutorial Mode
- **Manual Advancement**: Press 'E' to advance between game states (Deal -> Play -> Spin -> Action -> Post).
- **Fixed Setup**: Pre-defined initial cards for all participants.
- **Ordered Events**: A list of events and targets that Grim will follow round by round.
- **Visuals**: Maintains existing "Play your cards" messages and game logs. No additional tutorial-specific UI elements.

# Key Asset & Context
- `Assets/Main Game/Scripts/Card/GameManager.cs`: Primary logic controller.
- `Assets/Scripts/Managers/GameSettings.cs`: Global settings.
- `Assets/Main Game/Scripts/Tutorial/TutorialData.cs`: (To be created) ScriptableObject for round-by-round sequences.

# Implementation Steps

1. **Create Tutorial Data Structures**
    - **Description**: Create `TutorialData` ScriptableObject in `Assets/Main Game/Scripts/Tutorial/`. It should contain:
        - `List<ParticipantInitialSetup>`: Maps participant names to a list of initial `CardData`.
        - `List<ScriptedRound>`: A list where each entry defines the `EntityAction` and target `Participant` name for that round.
    - **Assigned role**: developer
    - **Dependencies**: None
    - **Parallelizable**: Yes

2. **Enhance GameManager for Scripted Execution**
    - **Description**:
        - Add `public TutorialData activeTutorial` and `private int currentRoundIndex`.
        - **Initial Dealing**: If `activeTutorial` is present, `DistributeInitialCards` should use the cards defined in `ParticipantInitialSetup` instead of the random pool.
        - **Grim Selection**: In `EntitySpinRoutine`, if `activeTutorial` has a `ScriptedRound` for the current index, set `selectedAction` and `currentTarget` directly from it.
        - **Manual Advancement**: Ensure `Update()` consistently uses 'E' to move from `PostEventWindow` to the next round and from `CardsPlay` to `EntitySpinning` when in Manual Mode.
    - **Assigned role**: developer
    - **Dependencies**: Step 1
    - **Parallelizable**: No

3. **Configure Tutorial Asset**
    - **Description**: Create a `TutorialData` asset in the project. Define the exact cards for the Player and Bots, and set up a sequence of events (e.g., Round 1: Shoot -> Player, Round 2: Russian Roulette -> Bot 1).
    - **Assigned role**: developer
    - **Dependencies**: Step 1
    - **Parallelizable**: Yes

4. **Assign and Test in Scene**
    - **Description**: Assign the new `TutorialData` asset to the `GameManager` in the `test` scene. Test the flow by entering Tutorial Mode from the Main Menu and verifying the sequence matches the asset.
    - **Assigned role**: developer
    - **Dependencies**: Step 2, Step 3
    - **Parallelizable**: No

# Verification & Testing
- **Card Distribution**: Start the tutorial and verify the player and bots have the exact cards specified in the asset.
- **Event Sequence**: Play through several rounds and confirm Grim executes the correct events on the correct targets in order.
- **Manual Progression**: Confirm the game waits for 'E' at each transition point as requested.
- **Regression**: Verify Normal Mode still uses `Resources.LoadAll` and random distribution as before.

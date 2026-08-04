# REPO Werewolf（REPO人狼）

![Match results screen](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/game-result.png)

A mod that adds an Among Us-style werewolf (social deduction) mode to R.E.P.O.


## Overview

Players are secretly assigned to either the villager team or the werewolf team, and the mind games unfold while everyone explores, collects, and delivers valuables as usual.
Werewolves sabotage from the shadows; villagers report bodies, call emergency meetings, and execute the werewolves by vote (or by force) before the quota becomes impossible to reach.

- Playable with 3 or more players
- Both the host and every client must have this mod installed
- UI languages: English (default) and Japanese

## Features

- Social deduction built on five roles — Villager, Shaman, Werewolf, Black Cat, and Bomber — each with its own team and special abilities
- Original rules where destroying and delivering valuables decides the match, built on R.E.P.O.'s core gameplay
- A meeting system that handles everything in-game: body reports, emergency meetings, discussion, voting, and executions
- Reshuffles the survivors into random groups after a meeting and scatters them across the map while enough of them are left
- Participant IDs, so you can say "No. 3 looks suspicious" in a public lobby
- A valuable loss gauge tracking deaths and valuables, plus a full meeting map with a coordinate grid
- Room settings for the stage, level, starting items, player upgrades, the truck's initial charge, and more
- Guests can review the host's room settings, and everyone's mod lineup is easy to check in the lobby
- A 27-page in-game manual covering the rules and every role's abilities, available at any time

## Meetings and Voting

![Meeting and voting screen](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/meeting-voting.png)

Meetings can be called by reporting a body or with the truck's emergency button.
Deaths and valuable losses/deliveries since the previous meeting are shared, and you can discuss and vote while checking the full map with its coordinate grid.

![Full map with coordinate grid](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/meeting-map-grid.png)

## Reviewing Room Settings and Mod Lineups in the Lobby

![Room settings and mod lineup check in the lobby](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/lobby-settings-and-mods.png)

Guests can review the room settings chosen by the host — stage, level, roles, starting items, and more — from the lobby.
The host's mod lineup also serves as the room baseline: every participant is compared against it, and missing mods, extra mods, version mismatches, and content differences are surfaced before the match starts.

## Roles

![Role lineup](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/roles.png)

- Villager (villager team): plays R.E.P.O. as usual — handling enemies, collecting valuables, advancing extractions — while hunting the werewolves through bodies, destroyed valuables, and other players' behavior
- Shaman (villager team): can stand still and sense whether unreported bodies exist and where, but is completely exposed while using the ability
- Werewolf (werewolf team): unlocks perks such as infinite stamina and extra jumps as valuables are destroyed, and lures enemies to players with the beacon
- Black Cat (werewolf team): belongs to the werewolf team but doesn't know who the werewolves are — and the werewolves don't know who the Black Cat is. Depending on the settings, being executed at a meeting takes someone down with them
- Bomber (werewolf team): secretly turns a player they've spent time near into a bomb, then detonates it from a distance at the perfect moment

## Mind Games

In REPO Werewolf, what you do while exploring becomes evidence at the meetings.
Cross-check your memory, everyone's testimony, and the map's records to decide whom to trust.
Be warned: melee weapons are tuned to reward ambushing other players, so choose carefully whom you let watch your back.
If meetings alone cannot settle the match, it moves to an endgame where both teams clash with the advantages they've built — weapons secured or confiscated, valuables destroyed.

## UI Language

The UI is available in English and Japanese, and English is the default — no setup is needed for English players.

To switch to Japanese:

1. Open `MODS` in the main menu
2. Find and select `Werewolf` in the mod list
3. Change `Language (restart required)` from `English` to `日本語`
4. Save with `SAVE CHANGES`
5. Restart the game

You can also edit the config file directly.

## Playing over Text Chat

![Meeting chat log](https://raw.githubusercontent.com/minorunara/repo-werewolf/main/docs/images/thunderstore/meeting-chat-log.png)

REPO Werewolf can be played entirely over text chat, without voice chat.

Use the game's chat key (default `T`) to open the normal chat and speak.
Messages sent during a meeting are recorded in the meeting chat log, which can be shown or hidden by clicking its icon or pressing its assigned key.

To type Japanese, install [JapaneseTextInput](https://thunderstore.io/c/repo/p/sukunabikona/JapaneseTextInput/).

## Installation

- Manual install: install the dependencies (BepInExPack, MenuLib, REPOConfig), then place `Minorunara_Werewolf.dll` in `BepInEx/plugins/Minorunara_Werewolf/` in the game folder.
- Every player in the lobby must install the mod.
- To keep everyone's mod lineup identical — for fairness and to avoid trouble — we recommend creating a dedicated profile with your mod manager and sharing it with all participants.
- This is a large mod and may conflict with others. If you want to run other mods alongside it, add them a few at a time and watch for issues.

## Suggested Room Settings for Your First Matches

| Players | Shaman | Werewolves | Black Cat | Bomber | Villager team + Werewolf team |
|---|---:|---:|---:|---:|---:|
| 3 | 0% | 1 | 0% | 0% | 2 + 1 |
| 4 | 50% | 1 | 0% | 0% | 3 + 1 |
| 5 | 100% | 1 | 100% (Revenge off) | 0% | 3 + 2 |
| 6 | 100% | 1 | 100% (Revenge on) | 0% | 4 + 2 |
| 7–8 | 100% | 2 | 0% | 50% | 5–6 + 2 |
| 9–10 | 100% | 2 | 100% (Revenge on) | 50% | 6–7 + 3 |

Vanilla rooms are capped at 6 players; to play with more you need a MorePlayers-type mod.
The Bomber appears at the configured probability by converting one of the Werewolves (at least one base Werewolf always remains, so no Bomber appears with only 1 werewolf).
The Black Cat converts from a Villager at the configured probability and joins the werewolf team (not counted in "Werewolves"; appears only in lineups that leave at least one Villager).
The Shaman converts from a Villager at the configured probability (the Shaman can never also be the Black Cat).
The Sledge Hammer and Gun have very high kill power; we recommend leaving them out of the loadout until your group can keep track of who is carrying which weapon.
As a rule of thumb: fewer upgrades, less stored energy, higher stage levels, and shorter time limits favor the werewolf team — and vice versa.

## In-Game Manual

> The following is identical to the in-game manual (default key: F1).
> Use it to review the rules before installing.

### Welcome to REPO Werewolf

In REPO Werewolf, players are split into the villager and werewolf teams, each working behind the scenes to secure victory.
The villager team's goal is the same as in regular R.E.P.O.: collect valuables, complete every extraction, and send the truck on its way.
The werewolf team's goal is to sabotage those efforts without revealing their identities.
Who suspects whom, and who is working with whom?
Can you uncover the truth amid all that suspicion? Or can you convince everyone that a lie is the truth?
The moment you seize on an easy answer and turn on your own, the players themselves become the most dangerous monsters of all.

### How a Match Works

REPO Werewolf requires at least 3 players.
In the room settings, the host can choose the stage and level, starting items, player upgrades, and more.
Warning: starting a match from an existing save file overwrites that save with the match state, erasing its previous contents.
There is no shop or next level—each match takes place in a single level.
Once either team secures victory, the results are shown and everyone returns to the lobby.
Cosmetic Boxes do not appear during a match. Instead, you can earn tokens afterward by meeting certain conditions.

### Villager Team Win Conditions

The villager team has two win conditions. Satisfy either one to win the match.  
・Complete every extraction and send the truck on its way with at least one villager still alive  
・Eliminate the werewolf team (the villagers still win if the Black Cat survives)  
The team of the player who starts the truck has no bearing on the result. What matters is whether any villagers are still alive when it departs.
In other words, completing the level as you normally would in R.E.P.O. also brings the villagers closer to victory.
The werewolf team will use every trick available to interfere, however, so this mode demands a different strategy from a normal run.

### Werewolf Team Win Conditions

The werewolf team has three win conditions. Satisfy any one to win the match.  
・Make the quota impossible to reach even if every available valuable were collected (value checkmate)  
・Keep the truck from departing until time runs out  
・Eliminate the villager team  
The moment value checkmate is reached, the werewolf team's victory is final.
Even after the final extraction is complete, the werewolf team wins if every villager dies before the truck departs.
This includes a Werewolf starting the truck and leaving every villager behind to die.

### Valuables and the Map

When a player discovers a valuable or an enemy drops an orb, a yellow marker is added to the map.
Unlike regular R.E.P.O., these markers do not update in real time under the default settings.
They update only when an extraction is completed or a meeting starts.
If no valuable is at a marker, someone either carried it away or destroyed it.
The map cannot tell you which.
Careful villagers take inventory at every meeting and ask where each missing valuable went.

### Not Recording Valuables (Werewolf Team)

A marker is added the moment a player sees a valuable.
By default, the werewolf team (Werewolves, Bombers, and awakened Black Cats) does not record newly discovered valuables.
If a member of the werewolf team reaches and destroys a valuable before anyone else finds it, the map shows no trace that it ever existed.
If a villager has already found and recorded it, however, the marker remains—alerting everyone that something is missing.
You can switch recording on and off at any time by holding the report key (the icon at the bottom right shows the current state).
Turn recording on as needed when you want to blend in with villagers while exploring.

### PvP

Unlike in regular R.E.P.O., melee weapons can damage players in Werewolf Mode, consuming weapon energy in the process (just as they do in the Super Smash Bros.-style arena).
There is also no safeguard that lets you survive an otherwise fatal blow at low health.
Melee attacks can disarm players as well.
When struck by a melee weapon, you drop whatever you are holding and one item is knocked out of your inventory.
You also cannot place an item in your inventory while another player is holding it. To take it, you must knock it out of their hands first.
In other words, whoever lands the first hit has the advantage. Choose carefully whom you trust to watch your back.

### Preparing for the Endgame

Late in the match, when meetings can no longer settle the conflict, the game may shift into an endgame where the werewolf team and the villager team face off directly.
How well each side can fight in the endgame depends on the position it has built over the course of the match.
Villagers can gain an advantage by securing and confiscating dangerous weapons to keep them out of the werewolf team's hands, protecting trusted allies, and keeping healing supplies in reserve.
The werewolf team can build its advantage by destroying valuables to unlock perks, killing the most trusted villagers, and disrupting the villagers' coordination.
The final confrontation is where the advantages both teams have built throughout the match come into play.
Keep the endgame in mind and prepare for it from the beginning.

### About Corpses

In REPO Werewolf, dead players cannot be revived by any means.
Body locations are not shown on the map, either. You must find them with your own eyes.
Press the report key (shown at the bottom right of the screen) near an unreported body to call a meeting on the spot.

### Calling a Meeting

There are two ways to call a meeting.  
・Grab and hold the red button at the back of the truck  
・Press the report key near an unreported body  
Each player can use the button to call a meeting once (this can be changed in the room settings).
The button is temporarily unavailable just after the match starts and after a meeting ends.
Once only one incomplete extraction point remains, meetings can no longer be called by reporting a body (for example, if the quota is 4 extraction points, reports are disabled after the 3rd is completed).
The button remains available until the very end.

### How Meetings Work

When a meeting starts, everyone is warped to the truck and immobilized.
During a meeting, enemies disappear and no new ones spawn, so you are safe.
Deaths since the previous meeting are announced first, followed by changes to the valuable loss gauge.
Voting begins afterward. During the meeting, you can open the full map to review the updated information on valuables.
The match time limit is paused during a meeting, so discussion does not eat into your remaining time.
Enemy respawn timers continue to count down, however, so an overly long meeting brings the enemies back sooner.
Any enemy whose respawn timer has expired by the end of the meeting spawns immediately.

### Voting and Execution

Each surviving player casts one vote at a meeting. Skipping (voting for no one) is also an option.
Everyone can see who has finished voting, but not who they voted for.
The player with the most votes is executed. If there is a tie for the most votes, no one is executed.
Meetings have a time limit, and each vote cast reduces the remaining time slightly.

### Reading the Valuable Loss Gauge

The valuable loss gauge tracks the struggle over valuables between the villager and werewolf teams. Its baseline is the total value of all valuables on the map when the match starts.

・Yellow bar (grows from the left)… total value lost through damage  
・Cyan bar (grows from the right)… total value delivered through completed extractions

・Blue line… the value required to meet the quota. Once the cyan bar reaches this line, the truck can depart  
・Red line… the value checkmate threshold, where the quota can no longer be met even if every available valuable is collected. The moment the yellow bar reaches this line, the werewolf team wins  
When an enemy drops an orb or new valuables appear on the map, the red line moves to the right, giving the villagers more leeway. This cannot overturn a checkmate victory once it is locked in.

### Role: Villager

Team: Villagers  
Villagers have no special abilities. Play as you would in regular R.E.P.O.: collect valuables and complete the extractions.
Your greatest weapons are observation and discussion. Compare notes on the valuables map, any bodies found, and what other players have done, then expose the Werewolves during meetings.

### Role: Shaman

Team: Villagers  
Base role: Villager  
Depending on the room settings, the Shaman is assigned from among the Villagers.
The Shaman can sense unreported bodies—those not yet revealed at a meeting.
They can infer the direction of a body and roughly when the player died.
Spirit vision requires standing still, leaving you vulnerable, and a haunting obscures your view when it sets in.
Be careful not to end up as the next body.

### Role: Shaman — Spirit Vision and Haunting

Spirit vision lets you sense the direction of a distant body.
Stand still and hold your gaze on one point. Your vision fades as spirit vision begins.
A dripping sound heard at regular intervals during spirit vision means there is no unreported body in that direction.
Keep looking toward an unreported body for several seconds, and your screen becomes heavily distorted for a moment.
Walls and distance do not matter. After the screen distorts, spirit vision will not react again until its cooldown ends. It is also unavailable briefly after the match starts.
If several unreported bodies exist, only the one nearest to you counts. Spirit vision will not react to any body farther away.
Moving or turning your gaze too far interrupts spirit vision. You cannot use it while a body is close enough to cause a haunting.
Your vision remains faded only while spirit vision is active.
Bodies already announced at a meeting produce no response.

A haunting lets you sense how close a nearby body is.
Near an unreported body, the haunting changes with distance across three levels: weak, medium, and strong.
Once only the final extraction point remains, the visual distortion disappears and only the sound remains.
Bodies already announced at a meeting produce no response.

### Role: Werewolf

Team: Werewolves  
The Werewolf role is the core of the werewolf team, and at least one player is always assigned this role.
Werewolves grow stronger as valuables are damaged.
Regardless of who caused the damage, perks unlock in sequence as the total value lost reaches each threshold.

Only players with the Werewolf role can see enemy positions on the map.

### Role: Werewolf — Perks

Infinite Stamina… dashing and ledge-grabbing no longer drain stamina

Extra Jumps… jump multiple times in midair (the number depends on the room settings)

Monster Camouflage… most enemies stop targeting you (they still react to sound)

Regeneration… while Wolf Mode is on, your health slowly recovers over time (no healing effect is shown, but the health gauge on your back is visible to other players)

Use the Wolf Mode key to turn perks on or off. Be careful—a villager who sees you use one will know what you are.

### Role: Werewolf — Beacon

The Beacon is a special ability of the Werewolf role. You gain one use each time valuables lose a certain total amount of value.
When activated, it emits a sound that players cannot hear and draws enemies from across the map to its location. It also brings back monsters that have been killed and despawned, subject to a revival cooldown shared by the entire werewolf team.
Use it to arrange an "accident" for an isolated player or send enemies after a group of villagers to cause panic.
However, this may create more available orbs and help the villagers reach their quota. You also risk being caught in the ensuing fight.
Using the Beacon is not announced immediately. At the next meeting, however, everyone is told how many times it has been used since the previous meeting.

### Role: Black Cat

Team: Werewolves (win/loss only)  
Base role: Villager  
Depending on the room settings, the Black Cat is assigned from among the Villagers.
At the start of the match, even the Black Cat is told they are a Villager. They awaken to their true identity after a short delay.
Otherwise, they have the same abilities as a Villager. If Black Cat's Revenge is enabled in the room settings, being executed at a meeting lets them choose one of their voters to take down with them. If they choose no one, a target is selected at random.
The Werewolves do not know who the Black Cat is, and initially the Black Cat does not know who the Werewolves are.

### Role: Black Cat — Informant

Informant… once valuables lose a certain total amount of value, the members of the werewolf team are revealed to the Black Cat alone.

The Black Cat can also see the valuable loss gauge, but it updates at intervals rather than in real time.

### Dealing with the Black Cat

When enabled in the room settings, the Black Cat's Revenge ability triggers only when they are executed by vote.
Do not rush to execute someone you suspect is the Black Cat. If you are right, one of the players who voted for them will be taken down too.
If an execution is unavoidable, only players prepared to be taken down should cast a vote.
The ability does not trigger if the Black Cat dies another way, such as to a weapon or a fall, but using force may ignite a fight.
If you kill someone without the group's agreement, the other villagers have no way to know whether you had good reason.
If your suspicion falls short of an execution, confiscating that player's weapons and watching them is another option. Without a weapon, even the werewolf team cannot easily kill anyone.

### Role: Bomber

Team: Werewolves  
Base role: Werewolf  
Depending on the room settings, the Bomber is assigned from among the Werewolves.
The Bomber can turn a nearby player into a bomb after spending enough time near them, then detonate the bomb at any time.

### Role: Bomber — Planting a Bomb

Plant Bomb is on cooldown at the start of the match and after each meeting. Once the cooldown ends, staying near another player gradually fills a yellow meter for that player. The meter does not fill through walls and drains when you move away.
When the meter is full and turns green, press the Plant Bomb key shown in the bottom-left corner of the screen to turn that player into a bomb.
A bomb icon visible only to the Bomber marks the affected player. They do not know they have been turned into a bomb.
The Bomber cannot turn themselves into a bomb, but can target a Werewolf or the Black Cat.
Only one player can be a bomb at a time. Before detonating it, you can reassign the bomb to another player.

### Role: Bomber — Detonation

Once the Detonate cooldown ends, press the key to trigger an explosion centered on the player you turned into a bomb.
The blast hits nearby players, valuables, and enemies. The bomb carrier also takes some damage, but their health never drops below 1 and they are not thrown.
If the Bomber is caught in the blast, they die instantly.
You cannot detonate while the target is near the truck.
Detonating after the target has died produces a dud, and the bomb is lost.
Your supply of bombs is replenished whenever valuables lose a certain total amount of value.

### When You Die

When you die, you enter spectator mode and watch the rest of the match play out.
The dead can talk to one another, but there is no way to pass information to the living—no Death Head tricks like in regular R.E.P.O.
Depending on the room settings, the werewolf team may hear the voices of the dead with an echo effect.
Victory and defeat are shared by the team, so you still win if your team wins, even after you die.

## Default Keybinds

These are the defaults on a fresh install.
For the current assignments, check the key prompts in-game or your own settings.

Keys added by this mod can be changed individually via your mod manager's config editor or in the `[Client Keybinds]` section of `BepInEx/config/minorunara.werewolf.cfg`.
Wolf Mode and Plant Bomb, as well as Beacon and Detonate, can each be bound to separate keys.

| Default key | Action | Config entry |
|---|---|---|
| F | Werewolf: Wolf Mode toggle (perks on/off) | `WolfModeKey` |
| F | Bomber: Plant Bomb | `BomberPlantKey` |
| G | Werewolf: Beacon | `BeaconKey` |
| G | Bomber: Detonate | `BomberDetonateKey` |
| R | Report a body (while near the head) | `CorpseReportKey` |
| M | Full map (during meetings) | `MeetingMapKey` |
| L | Meeting chat log (during meetings) | `MeetingChatLogKey` |
| F1 | In-game manual | `ManualKey` |
| F5 | Return to lobby from the match results (host only) | `ResultReturnKey` |
| F7 | Show/hide the lobby settings panel | `LobbySettingsPanelKey` |

## Notes

- Werewolf Mode requires at least 3 players.
  Trying to start with fewer shows a warning and the match will not start
- Setting the werewolf count (WerewolfCount) to the number of participants or higher also shows a warning and blocks the start (a role assignment with no villagers is impossible; lower the setting to proceed)
- To play regular R.E.P.O., the host can turn off Werewolf Mode (WerewolfModeEnabled) in the settings (the host's setting decides whether the room runs Werewolf Mode; a guest's setting applies only when that player hosts)

## Streamer-Safe Mode

For anyone who wants to avoid automated content detection on streaming platforms or misunderstandings from viewers, this mode replaces some parody visuals and distinctive sound effects with generic assets or silence. Set `StreamerSafeMode = true` in the `[Streamer]` section of the config file (`BepInEx/config/minorunara.werewolf.cfg`); the change takes effect after a restart:

- The Bomber's two ability icons (parody visuals) → generic bomb icons
- The meeting-convene chime → a generic notification sound
- The two execution chants → silence (the chants themselves are royalty-free assets [see African Mist Voice under "Credits"], but they are widely recognized from a well-known game, so they are included here)

Gameplay and the timing of every effect stay the same. This is a local setting for each player and only affects your own screen and audio, so it is enough for the streamer alone to turn it on.

## Credits

- [OtoLogic](https://otologic.jp/) ([CC BY 4.0 / Terms](https://otologic.jp/free/license))
- [Sound Effect Lab（効果音ラボ）](https://soundeffect-lab.info/) ([Terms](https://soundeffect-lab.info/agreement/))
- [Irasutoya（いらすとや）](https://www.irasutoya.com/) ([Terms](https://www.irasutoya.com/p/terms.html))
- [Vita-chi Sozaikan（びたちー素材館）](http://www.vita-chi.net/sozai1.htm)
- [African Mist Voice | No Copyright Music | Royalty Free Loops |](https://www.youtube.com/watch?v=UvmdiB_7YX0)

## License

The source code of this mod is released under the [MIT License](./LICENSE).
Third-party assets embedded in the released mod (the sounds and images credited above) remain under their respective original terms and are not covered by the MIT License.

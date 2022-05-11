using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PauseInMultiplayer
{
    public class ModEntry : Mod
    {
        ModConfig Config;

        Color textColor = Game1.textColor;

        bool pauseTime = false;
        System.Collections.Generic.IDictionary<long, bool>? pauseTimeAll;

        bool inSkull = false;
        System.Collections.Generic.IDictionary<long, bool>? inSkullAll;
        bool inSkullLast = false;
        bool extraTimeAdded = false;
        int lastSkullLevel = 121;

        bool lastEventCheck = false;
        System.Collections.Generic.IDictionary<long, bool>? eventStatus;

        int healthLock = -100;
        System.Collections.Generic.Dictionary<StardewValley.Monsters.Monster, Vector2> monsterLocks = new System.Collections.Generic.Dictionary<StardewValley.Monsters.Monster, Vector2>();
        bool lockMonsters = false;
        int timeInterval = -100;
        int foodDuration = -100;
        int drinkDuration = -100;

        bool pauseCommand = false;
        bool shouldPauseLast = false;

        bool pasekSetupMode = false;

        //Pasek felds and assets
        Texture2D? Pasek;
        Texture2D? PasekWithUIS;
        Texture2D? PasekZoom;
        Texture2D? Black;
        Texture2D? Blue;
        Texture2D? Green;
        Texture2D? Red;

        Vector2 PasekPosition = new(44, 248);
        Vector2 PasekPositionColor = new(68, 272);
        Vector2 UiInfoHooksPosition = new(44, 202);
        Vector2 PasekZoomPositionColor = new(68, 300);

        //Additional methods
        private bool ShouldPause()
        {
            try
            {
                if (Context.IsMainPlayer && pauseTimeAll != null)
                {
                    //normal pause logic (terminates via false)
                    foreach (bool pauseTime in pauseTimeAll.Values)
                        if (!pauseTime) return false;

                    return true;
                }
                else
                {
                    return pauseCommand;
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log(ex.Message, LogLevel.Debug);
                this.Monitor.Log("Reinitializing pauseCommand.", LogLevel.Debug);
                pauseCommand = false;
                return false;
            }
        }

        private bool AllInSkull()
        {
            if (Context.IsMainPlayer && inSkullAll != null)
            {
                foreach (bool inSkull in inSkullAll.Values)
                    if (!inSkull) return false;

                return true;
            }

            return false;
        }

        private void DrawFade(SpriteBatch b)
        {
            string text = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth) + ". " + Game1.dayOfMonth;
            Vector2 dayPosition = new Vector2((float)Math.Floor(183.5f - Game1.dialogueFont.MeasureString(text).X / 2), (float)18);
            b.DrawString(Game1.dialogueFont, text, Game1.dayTimeMoneyBox.position + dayPosition, textColor);
            string timeofDay = (Game1.timeOfDay < 1200 || Game1.timeOfDay >= 2400) ? " am" : " pm";
            string zeroPad = (Game1.timeOfDay % 100 == 0) ? "0" : "";
            string hours = (Game1.timeOfDay / 100 % 12 == 0) ? "12" : string.Concat(Game1.timeOfDay / 100 % 12);
            string time = string.Concat(new object[]
            {
                hours,
                ":",
                Game1.timeOfDay % 100,
                zeroPad,
                timeofDay
            });
            Vector2 timePosition = new Vector2((float)Math.Floor(183.5 - Game1.dialogueFont.MeasureString(time).X / 2), (float)108);
            bool nofade = !ShouldPause() || Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 2000.0 > 1000.0;
            b.DrawString(Game1.dialogueFont, time, Game1.dayTimeMoneyBox.position + timePosition, (Game1.timeOfDay >= 2400) ? Color.Red : (textColor * (nofade ? 1f : 0.5f)));
        }

        private void DrawPasek(SpriteBatch b)
        {
            int width = (int)(108 - (Game1.getOnlineFarmers().Count - 1) * 4) / Game1.getOnlineFarmers().Count;
            int i = 0;
            if (this.Config.UiInfoSuite)
            {
                b.Draw(PasekWithUIS, Game1.dayTimeMoneyBox.position + UiInfoHooksPosition, null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
            }
            if (this.Config.ZoomButtons)
            {
                b.Draw(PasekZoom, Game1.dayTimeMoneyBox.position + PasekPosition, null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    if (!pauseTimeAll![farmer.UniqueMultiplayerID])
                    {
                        b.Draw(Blue, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                        if (Context.IsMainPlayer)
                        {
                            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
                            {
                                if (peer.GetMod(this.ModManifest.UniqueID) == null && peer.PlayerID == farmer.UniqueMultiplayerID)
                                {
                                    b.Draw(Red, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                                }
                            }
                        }
                    }
                    else
                    {
                        b.Draw(Green, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    if (i != Game1.getOnlineFarmers().Count - 1)
                    {
                        b.Draw(Black, Game1.dayTimeMoneyBox.position + PasekZoomPositionColor + new Vector2(i * (width + 4) + width, 0), new Rectangle(i * (width + 4) + width, 0, 4, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    i++;
                }
            }
            else
            {
                foreach (Farmer farmer in Game1.getOnlineFarmers())
                {
                    if (!pauseTimeAll![farmer.UniqueMultiplayerID])
                    {
                        b.Draw(Blue, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                        if (Context.IsMainPlayer)
                        {
                            foreach (IMultiplayerPeer peer in Helper.Multiplayer.GetConnectedPlayers())
                            {
                                if (peer.GetMod(this.ModManifest.UniqueID) == null)
                                {
                                    if (peer.PlayerID == farmer.UniqueMultiplayerID)
                                    {
                                        b.Draw(Red, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        b.Draw(Green, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4), 0), new Rectangle(0, 0, width, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    if (i != Game1.getOnlineFarmers().Count - 1)
                    {
                        b.Draw(Black, Game1.dayTimeMoneyBox.position + PasekPositionColor + new Vector2(i * (width + 4) + width, 0), new Rectangle(i * (width + 4) + width, 0, 4, 24), Color.White, 0.0f, Vector2.Zero, 1, SpriteEffects.None, 0.99f);
                    }
                    i++;
                }
                b.Draw(Pasek, Game1.dayTimeMoneyBox.position + PasekPosition, null, Color.White, 0.0f, Vector2.Zero, 4, SpriteEffects.None, 0.99f);
            }
        }

        //Main methods
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            //Loading Pasek assets
            this.Pasek = Helper.ModContent.Load<Texture2D>("assets/Pasek.png");
            this.PasekWithUIS = Helper.ModContent.Load<Texture2D>("assets/PasekWithUIS.png");
            this.PasekZoom = Helper.ModContent.Load<Texture2D>("assets/PasekZoom.png");
            this.Black = Helper.ModContent.Load<Texture2D>("assets/Black.png");
            this.Blue = Helper.ModContent.Load<Texture2D>("assets/not-paused.png");
            this.Green = Helper.ModContent.Load<Texture2D>("assets/paused.png");
            this.Red = Helper.ModContent.Load<Texture2D>("assets/Red.png");
            if (this.Config.UiInfoSuite)
            {
                PasekPosition.Y += 42;
                PasekPositionColor.Y += 42;
                PasekZoomPositionColor.Y += 42;
            }

            //checks if "Skull Cavern Elevator" mod is in use
            bool skullElevatorMod = Helper.ModRegistry.Get("SkullCavernElevator") != null;
            if (skullElevatorMod)
            {
                this.Monitor.Log("DisableSkullShaftFix set to true due to SkullCavernElevator mod.", LogLevel.Debug);
                this.Config.DisableSkullShaftFix = true;
            }

            Helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            Helper.Events.GameLoop.UpdateTicking += this.GameLoop_UpdateTicking;
            Helper.Events.GameLoop.Saving += this.GameLoop_Saving;
            Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;

            Helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;
            Helper.Events.Multiplayer.PeerConnected += this.Multiplayer_PeerConnected;
            Helper.Events.Multiplayer.PeerDisconnected += this.Multiplayer_PeerDisconnected;

            Helper.Events.Display.RenderingHud += this.PreRenderHud;
            Helper.Events.Display.RenderedHud += this.PostRenderHud;
            Helper.Events.Display.Rendered += this.Display_Rendered;

            Helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;

            Helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
        }

        private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            //GMCM support
            var configMenu = this.Helper.ModRegistry.GetApi<GenericModConfigMenu.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) return;

            configMenu.Register(mod: this.ModManifest, reset: () => this.Config = new ModConfig(), save: () => this.Helper.WriteConfig(this.Config));

            bool skullElevatorMod = Helper.ModRegistry.Get("SkullCavernElevator") != null;
            if (skullElevatorMod)
            {
                this.Monitor.Log("DisableSkullShaftFix set to true due to SkullCavernElevator mod.", LogLevel.Debug);
                this.Config.DisableSkullShaftFix = true;
            }

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Pasek Settings"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show Pasek",
                tooltip: () => "Toggles whether or not to show the pasek.",
                getValue: () => this.Config.UiInfoSuite,
                setValue: value => this.Config.ShowPasek = value
            );

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Toggle pasek key",
                tooltip: () => "Select the keybind for showing/hiding the pasek.",
                getValue: () => this.Config.PasekToggleButton,
                setValue: value => this.Config.PasekToggleButton = value
            );

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Toggle pasek setup key",
                tooltip: () => "Togles wether the pasek can be moved or not.",
                getValue: () => this.Config.PasekToggleSetupMode,
                setValue: value => this.Config.PasekToggleSetupMode = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "UI Info Suite",
                tooltip: () => "Adjusts the pasek if you use UI Info Suite mod, to not conflict with the icons.",
                getValue: () => this.Config.UiInfoSuite,
                setValue: value => this.Config.UiInfoSuite = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Zoom Buttons",
                tooltip: () => "Adjusts the pasek if having zoom buttons enabled.",
                getValue: () => this.Config.ZoomButtons,
                setValue: value => this.Config.ZoomButtons = value
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Local Settings"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Disable Skull Cavern shaft fix",
                tooltip: () => "Only set this to true if you have a specific reason to, such as using the Skull Cavern elevator mod.",
                getValue: () => this.Config.DisableSkullShaftFix,
                setValue: value => this.Config.DisableSkullShaftFix = value
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "Host-Only Settings"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Fix Skull Cavern time",
                tooltip: () => "(host only)\nToggles whether or not to slow down time like in single-player if all players are in the Skull Cavern.",
                getValue: () => this.Config.FixSkullTime,
                setValue: value => this.Config.FixSkullTime = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Monster/HP pause lock",
                tooltip: () => "(host only)\nToggles whether or not monsters will freeze and health will lock while paused.",
                getValue: () => this.Config.LockMonsters,
                setValue: value => this.Config.LockMonsters = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Any cutscene pauses",
                tooltip: () => "(host only)\nWhen enabled, time will pause if any player is in a cutscene.",
                getValue: () => this.Config.AnyCutscenePauses,
                setValue: value => this.Config.AnyCutscenePauses = value
            );
        }

        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            //only the main player will use this dictionary
            if (Context.IsMainPlayer)
            {
                pauseTimeAll = new System.Collections.Generic.Dictionary<long, bool>();
                pauseTimeAll[Game1.player.UniqueMultiplayerID] = false;

                inSkullAll = new System.Collections.Generic.Dictionary<long, bool>();
                inSkullAll[Game1.player.UniqueMultiplayerID] = false;

                //setup lockMonsters for main player
                lockMonsters = this.Config.LockMonsters;

                eventStatus = new System.Collections.Generic.Dictionary<long, bool>();
                eventStatus[Game1.player.UniqueMultiplayerID] = false;
            }
        }

        private void GameLoop_Saving(object? sender, SavingEventArgs e)
        {
            //reset invincibility settings while saving to help prevent future potential errors if the mod is disabled later
            //redundant with Saving to handle farmhand inconsistency
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.player.currentTemporaryInvincibilityDuration = 0;
            Game1.player.temporarilyInvincible = false;
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            //reset invincibility settings while saving to help prevent future potential errors if the mod is disabled later
            //redundant with DayEnding to handle farmhand inconsistency
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.player.currentTemporaryInvincibilityDuration = 0;
            Game1.player.temporarilyInvincible = false;
        }

        private void GameLoop_UpdateTicking(object? sender, UpdateTickingEventArgs e)
        {//this mod does nothing if a game isn't running
            if (!Context.IsWorldReady) return;

            //start with checking for events
            if (lastEventCheck != Game1.eventUp)
            {
                //host
                if (Context.IsMainPlayer && eventStatus != null)
                {
                    eventStatus[Game1.player.UniqueMultiplayerID] = Game1.eventUp;
                }
                //client
                else
                {
                    this.Helper.Multiplayer.SendMessage<bool>(Game1.eventUp, "eventUp", modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
                }
                lastEventCheck = Game1.eventUp;
            }

            //set the pause time data to whether or not time should be paused for this player
            var pauseTime2 = !Context.IsPlayerFree;

            if (!Context.CanPlayerMove)
                pauseTime2 = true;

            //time should not be paused when using a tool
            if (Game1.player.UsingTool)
                pauseTime2 = false;

            //checks to see if the fishing rod has been cast. If this is true but the player is in the fishing minigame, the next if statement will pause - otherwise it won't
            if (Game1.player.CurrentItem != null && Game1.player.CurrentItem is StardewValley.Tools.FishingRod && (Game1.player.CurrentItem as StardewValley.Tools.FishingRod)!.isFishing)
                pauseTime2 = false;

            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is StardewValley.Menus.BobberBar)
                pauseTime2 = true;

            if (Game1.currentMinigame != null)
                pauseTime2 = true;

            //Skull Cavern logic - skip skull cavern fix logic if the main player has it disabled, or if is not multiplayer
            if (!Game1.IsMultiplayer || (Context.IsMainPlayer && !this.Config.FixSkullTime))
            {
                //check status to see if player is in Skull Cavern
                if (Game1.player.currentLocation is StardewValley.Locations.MineShaft && (Game1.player.currentLocation as StardewValley.Locations.MineShaft)!.getMineArea() > 120)
                    inSkull = true;
                else
                    inSkull = false;

                if (inSkull != inSkullLast)
                {
                    if (Context.IsMainPlayer && inSkullAll != null)
                        inSkullAll[Game1.player.UniqueMultiplayerID] = inSkull;
                    else
                        this.Helper.Multiplayer.SendMessage(inSkull, "inSkull", modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });

                    inSkullLast = inSkull;
                }

                //apply the logic to remove 2000 from the time interval if everyone is in the skull cavern and this hasn't been done yet per this 10 minute day period
                if (Context.IsMainPlayer && Game1.gameTimeInterval > 6000 && AllInSkull())
                {
                    if (!extraTimeAdded)
                    {
                        extraTimeAdded = true;
                        Game1.gameTimeInterval -= 2000;
                    }
                }

                if (Context.IsMainPlayer && Game1.gameTimeInterval < 10)
                    extraTimeAdded = false;
            }
            //End Skull Cavern logic

            //handle pause time data
            if (pauseTime != pauseTime2)
            {
                //host
                if (Context.IsMainPlayer && pauseTimeAll != null)
                {
                    pauseTime = pauseTime2;
                    pauseTimeAll[Game1.player.UniqueMultiplayerID] = pauseTime;
                }

                //client
                else
                {
                    pauseTime = pauseTime2;
                    this.Helper.Multiplayer.SendMessage(pauseTime, "pauseTime", modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
                }
            }

            var shouldPauseNow = ShouldPause();

            //this logic only applies for the main player to control the state of the world
            if (Context.IsMainPlayer)
            {
                if (shouldPauseNow)
                {

                    //save the last time interval, if it's not already saved
                    if (Game1.gameTimeInterval >= 0)
                        timeInterval = Game1.gameTimeInterval;

                    Game1.gameTimeInterval = -100;

                    //pause all Characters
                    foreach (GameLocation location in Game1.locations)
                    {
                        //I don't know if the game stores null locations, and at this point I'm too afraid to ask
                        if (location == null) continue;

                        //pause all NPCs, doesn't seem to work for animals or monsters
                        foreach (Character character in location.characters)
                        {
                            character.movementPause = 1;
                        }

                        //pause all farm animals
                        if (location is Farm)
                            foreach (FarmAnimal animal in (location as Farm)!.getAllFarmAnimals())
                                animal.pauseTimer = 100;
                        else if (location is AnimalHouse)
                            foreach (FarmAnimal animal in (location as AnimalHouse)!.animals.Values)
                                animal.pauseTimer = 100;
                    }

                    //pause all Monsters
                    if (lockMonsters)
                    {
                        System.Collections.Generic.List<GameLocation> farmerLocations = new System.Collections.Generic.List<GameLocation>();

                        foreach (Farmer f in Game1.getOnlineFarmers())
                            farmerLocations.Add(f.currentLocation);

                        foreach (GameLocation location in farmerLocations)
                        {
                            foreach (Character character in location.characters)
                            {
                                if (character != null && character is StardewValley.Monsters.Monster)
                                {
                                    if (!monsterLocks.ContainsKey((character as StardewValley.Monsters.Monster)!))
                                        monsterLocks.Add((character as StardewValley.Monsters.Monster)!, character.Position);

                                    character.Position = monsterLocks[(character as StardewValley.Monsters.Monster)!];
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //reset time interval if it hasn't been fixed from the last pause
                if (Game1.gameTimeInterval < 0)
                {
                    Game1.gameTimeInterval = timeInterval;
                    timeInterval = -100;
                }

                //reset monsterLocks
                monsterLocks.Clear();
            }

            if (shouldPauseNow != shouldPauseLast)
            {
                this.Helper.Multiplayer.SendMessage(shouldPauseNow, "pauseCommand", new[] { this.ModManifest.UniqueID });
            }

            shouldPauseLast = shouldPauseNow;

            //check if the player has jumped down a Skull Cavern Shaft
            if (!this.Config.DisableSkullShaftFix && inSkull)
            {
                GameLocation currentLocation = Game1.player.currentLocation;
                if (currentLocation is StardewValley.Locations.MineShaft)
                {
                    int num = (currentLocation as StardewValley.Locations.MineShaft)!.mineLevel - lastSkullLevel;
                    if (num > 1)
                    {

                        if (healthLock != -100)
                        {
                            Game1.player.health = Math.Max(1, Game1.player.health - num * 3);
                            healthLock = Game1.player.health;
                        }

                    }

                    lastSkullLevel = (currentLocation as StardewValley.Locations.MineShaft)!.mineLevel;
                }
            }


            //pause food and drink buff durations must be run for each player independently
            //handle health locks on a per player basis
            if (shouldPauseNow)
            {
                //set temporary duration locks if it has just become paused and/or update Duration if new food is consumed during pause
                if (Game1.buffsDisplay.food != null && Game1.buffsDisplay.food.millisecondsDuration > foodDuration)
                    foodDuration = Game1.buffsDisplay.food.millisecondsDuration;
                if (Game1.buffsDisplay.drink != null && Game1.buffsDisplay.drink.millisecondsDuration > drinkDuration)
                    drinkDuration = Game1.buffsDisplay.drink.millisecondsDuration;

                if (Game1.buffsDisplay.food != null)
                    Game1.buffsDisplay.food.millisecondsDuration = foodDuration;
                if (Game1.buffsDisplay.drink != null)
                    Game1.buffsDisplay.drink.millisecondsDuration = drinkDuration;

                if (lockMonsters)
                {
                    //health lock
                    if (healthLock == -100)
                        healthLock = Game1.player.health;
                    //catch edge cases where health has increased but asynchronously will not be applied before locking
                    if (Game1.player.health > healthLock)
                        healthLock = Game1.player.health;

                    Game1.player.health = healthLock;

                    Game1.player.temporarilyInvincible = true;
                    Game1.player.temporaryInvincibilityTimer = -1000000000;
                }
            }
            else
            {
                foodDuration = -100;
                drinkDuration = -100;

                healthLock = -100;

                if (Game1.player.temporaryInvincibilityTimer < -100000000)
                {
                    Game1.player.temporaryInvincibilityTimer = 0;
                    Game1.player.currentTemporaryInvincibilityDuration = 0;
                    Game1.player.temporarilyInvincible = false;
                }
            }

            if(Context.IsMainPlayer)
            {
                this.Helper.Multiplayer.SendMessage(this.pauseTimeAll, "updatePauseData", modIDs: new[] { this.ModManifest.UniqueID });
            }
        }

        private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {

            if (!Context.IsWorldReady) return;

            else if (e.Button == this.Config.PasekToggleButton)
                this.Config.ShowPasek = !this.Config.ShowPasek;

            else if (e.Button == this.Config.PasekToggleSetupMode)
                this.pasekSetupMode = !this.pasekSetupMode;

            else if (this.pasekSetupMode && e.Button == SButton.Up)
            {
                PasekPosition.Y += 1;
                PasekPositionColor.Y += 1;
                PasekZoomPositionColor.Y += 1;
            }

            else if (this.pasekSetupMode && e.Button == SButton.Down)
            {
                PasekPosition.Y += -1;
                PasekPositionColor.Y += -1;
                PasekZoomPositionColor.Y += -1;
            }

            else if (this.pasekSetupMode && e.Button == SButton.Left)
            {
                PasekPosition.X += -1;
                PasekPositionColor.X += -1;
                PasekZoomPositionColor.X += -1;
            }

            else if (this.pasekSetupMode && e.Button == SButton.Right)
            {
                PasekPosition.X += 1;
                PasekPositionColor.X += 1;
                PasekZoomPositionColor.X += 1;
            }
        }

        private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                if (e.FromModID == this.ModManifest.UniqueID)
                {
                    if (e.Type == "pauseTime" && pauseTimeAll != null)
                        pauseTimeAll[e.FromPlayerID] = e.ReadAs<bool>();

                    else if (e.Type == "inSkull" && inSkullAll != null)
                        inSkullAll[e.FromPlayerID] = e.ReadAs<bool>();

                    else if (e.Type == "eventUp" && eventStatus != null)
                        eventStatus[e.FromPlayerID] = e.ReadAs<bool>();
                }
            }
            else
            {
                if (e.FromModID == this.ModManifest.UniqueID && e.Type == "pauseCommand")
                {
                    this.pauseCommand = e.ReadAs<bool>();
                }
                else if (e.FromModID == this.ModManifest.UniqueID && e.Type == "lockMonsters")
                {
                    this.lockMonsters = e.ReadAs<bool>();
                }
                else if(e.FromModID == this.ModManifest.UniqueID && e.Type == "updatePauseData")
                {
                    this.pauseTimeAll = e.ReadAs<System.Collections.Generic.IDictionary<long, bool>>();
                }
            }
        }

        private void Multiplayer_PeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                pauseTimeAll![e.Peer.PlayerID] = false;
                inSkullAll![e.Peer.PlayerID] = false;
                eventStatus![e.Peer.PlayerID] = false;

                //send current pause stat
                this.Helper.Multiplayer.SendMessage(shouldPauseLast ? true : false, "pauseCommand", new[] { this.ModManifest.UniqueID }, new[] { e.Peer.PlayerID });

                //send message denoting whether or not monsters will be locked
                this.Helper.Multiplayer.SendMessage(lockMonsters ? true : false, "lockMonsters", modIDs: new[] { this.ModManifest.UniqueID }, playerIDs: new[] { e.Peer.PlayerID });


                //check for version match
                IMultiplayerPeerMod? pauseMod;
                pauseMod = e.Peer.GetMod(this.ModManifest.UniqueID);
                if (pauseMod == null)
                    Game1.chatBox.addErrorMessage("Farmhand " + Game1.getFarmer(e.Peer.PlayerID).Name + " does not have Pause in Multiplayer mod.");
                else if (!pauseMod.Version.Equals(this.ModManifest.Version))
                {
                    Game1.chatBox.addErrorMessage("Farmhand " + Game1.getFarmer(e.Peer.PlayerID).Name + " has mismatched Pause in Multiplayer version.");
                    Game1.chatBox.addErrorMessage($"Host Version: {this.ModManifest.Version} | {Game1.getFarmer(e.Peer.PlayerID).Name} Version: {pauseMod.Version}");
                }

                this.Helper.Multiplayer.SendMessage(this.pauseTimeAll, "updatePauseData", modIDs: new[] { this.ModManifest.UniqueID });
            }
        }

        private void Multiplayer_PeerDisconnected(object? sender, PeerDisconnectedEventArgs e)
        {
            if (Context.IsMainPlayer)
            {
                pauseTimeAll!.Remove(e.Peer.PlayerID);
                inSkullAll!.Remove(e.Peer.PlayerID);
                eventStatus!.Remove(e.Peer.PlayerID);
            }
        }

        private void PreRenderHud(object? sender, RenderingHudEventArgs e)
        {
            if (ShouldPause())
            {
                Game1.textColor *= 0f;
                Game1.dayTimeMoneyBox.timeShakeTimer = 0;
            }
            if (Context.IsMultiplayer && !Game1.isFestival() && this.Config.ShowPasek)
            {
                DrawPasek(Game1.spriteBatch);
            }
        }

        private void PostRenderHud(object? sender, RenderedHudEventArgs e)
        {
            if (ShouldPause())
            {
                DrawFade(Game1.spriteBatch);
            }
            Game1.textColor = textColor;
        }

        private void Display_Rendered(object? sender, RenderedEventArgs e)
        {
            if (Context.IsWorldReady)
            {
                if (!Game1.isFestival() && !(Game1.farmEvent != null &&
                                                (Game1.farmEvent is StardewValley.Events.FairyEvent ||
                                                 Game1.farmEvent is StardewValley.Events.WitchEvent ||
                                                 Game1.farmEvent is StardewValley.Events.SoundInTheNightEvent))
                                        && (Game1.eventUp ||
                                            Game1.currentMinigame != null ||
                                            Game1.activeClickableMenu is StardewValley.Menus.AnimalQueryMenu ||
                                            Game1.activeClickableMenu is StardewValley.Menus.PurchaseAnimalsMenu ||
                                            Game1.activeClickableMenu is StardewValley.Menus.CarpenterMenu ||
                                            Game1.freezeControls))
                {
                    Game1.textColor *= 0f;
                    Game1.dayTimeMoneyBox.timeShakeTimer = 0;
                    Game1.spriteBatch.End();
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                    Game1.dayTimeMoneyBox.draw(Game1.spriteBatch);
                    Game1.textColor = textColor;
                    DrawFade(Game1.spriteBatch);
                    if (Context.IsMultiplayer && this.Config.ShowPasek)
                    {
                        DrawPasek(Game1.spriteBatch);
                    }
                }
            }
        }

        public class ModConfig
        {
            public bool ShowPasek { get; set; } = false;

            public SButton PasekToggleButton { get; set; } = SButton.J;

            public SButton PasekToggleSetupMode { get; set; } = SButton.K;

            public bool UiInfoSuite { get; set; } = false;

            public bool ZoomButtons { get; set; } = false;

            public bool FixSkullTime { get; set; } = true;

            public bool DisableSkullShaftFix { get; set; } = false;

            public bool LockMonsters { get; set; } = true;

            public bool AnyCutscenePauses { get; set; } = false;
        }
    }
}
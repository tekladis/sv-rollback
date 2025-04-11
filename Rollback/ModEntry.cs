using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Rollback;

internal sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    
    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
//        helper.Events.GameLoop.UpdateTicked += this.InitializeSettings;
    }
    
    // Configuration menu
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null) return;
        configMenu.Register(
            mod: this.ModManifest,
            reset: () => _config = new ModConfig(),
            save: () => this.Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Fence Auto Repair",
            tooltip: () => "Repairs fences at end of the day",
            getValue: () => _config.RepairFences,
            setValue: value => _config.RepairFences = value);

        /*        
                this.Monitor.Log($"{String.Join(",", allowedLocations.ToArray())}", LogLevel.Alert);

                configMenu.AddTextOption(
                    mod: this.ModManifest,
                    name: () => "Fence Locations",
                    tooltip: () => "Repairs fences at end of the day",
                    getValue: () => string.Join(",", _config.FenceLocations),
                    setValue: value => _config.FenceLocations = value.ToString().Split(","),
                    allowedValues: allowedLocations.ToArray());
          */
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Allow Dirt Decay",
            tooltip: () => "Allows decay of tilled dirt",
            getValue: () => _config.DisableDirtDecay,
            setValue: value => _config.DisableDirtDecay = value);
        
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Allow Grass Growth",
            tooltip: () => "Allows grass to spread",
            getValue: () => _config.DisableGrassGrowth,
            setValue: value => _config.DisableGrassGrowth = value);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
    }

    // TODO: Extra features
    /*
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
    }
*/
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.Helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
        
        foreach (string entry in _config.FenceLocations)
        {
            GameLocation? gl = Game1.getLocationFromName(entry);
            if (gl == null) continue;
            foreach (Fence f in gl.Objects.Values.OfType<Fence>())
            {
                f.setHealth((int)f.maxHealth.Value);
            }
        }
    }

    private void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e)
    {
        if (_config.DisableGrassGrowth)
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> p in e.Added)
            {
                if (p.Value is Grass)
                {
                    e.Location.removeObject(p.Key, false);
                }
            }
        }

        if (_config.DisableDirtDecay)
        {
            foreach (KeyValuePair<Vector2, TerrainFeature> p in e.Removed)
            {
                if (p.Value is HoeDirt)
                {
                    e.Location.makeHoeDirt(p.Key);
                }
            }
        }
        
        // Remove this event since we're done for the day
        this.Helper.Events.World.TerrainFeatureListChanged -= this.OnTerrainFeatureListChanged;
    }
}
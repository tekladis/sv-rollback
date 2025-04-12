using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Rollback;

internal sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    
    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
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

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Disable Dirt Decay",
            tooltip: () => "Prevents decay of tilled dirt",
            getValue: () => _config.DisableDirtDecay,
            setValue: value => _config.DisableDirtDecay = value);
        
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Disable Grass Growth",
            tooltip: () => "Prevents grass from spreading",
            getValue: () => _config.DisableGrassGrowth,
            setValue: value => _config.DisableGrassGrowth = value);
        
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Disable Weed Growth",
            tooltip: () => "Prevents weeds from growing entirely",
            getValue: () => _config.DisableWeedGrowth,
            setValue: value => _config.DisableWeedGrowth = value);
        
        configMenu.AddBoolOption(
            mod: this.ModManifest,
            name: () => "Disable Tree Spread",
            tooltip: () => "Prevents trees from spreading to nearby tiles",
            getValue: () => _config.DisableTreeSpread,
            setValue: value => _config.DisableTreeSpread = value);
    }
    
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        // Apply event handlers to run once
        this.Helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
        this.Helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        
        // Repair fences
        foreach (string entry in _config.FenceLocations)
        {
            GameLocation? gl = Game1.getLocationFromName(entry);
            if (gl == null) continue;
            foreach (Fence f in gl.Objects.Values.OfType<Fence>())
            {
                f.repair();
            }
        }
        
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        foreach (KeyValuePair<Vector2, Object> p in e.Added)
        {
            if (_config.DisableWeedGrowth && p.Value.IsWeeds())
            {
                e.Location.removeObject(p.Value.TileLocation, false);
                
                // Prevent alterations to in-game stats
                --Game1.stats.WeedsEliminated;
            }
        }
        
        this.Helper.Events.World.ObjectListChanged -= this.OnObjectListChanged;
    }

    private void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e)
    {
        foreach ((Vector2 pos, TerrainFeature? feature) in e.Added)
        {
            switch (feature)
            {
                case Tree t when _config.DisableTreeSpread && t.growthStage.Value == 0:
                case Grass when _config.DisableGrassGrowth:
                    e.Location.removeObject(pos, false);
                    break;
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
        
        this.Helper.Events.World.TerrainFeatureListChanged -= this.OnTerrainFeatureListChanged;
    }
}
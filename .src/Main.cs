using System.Numerics;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CS2TraceRay.Class;
using CS2TraceRay.Struct;
using System.Text.Json;
using System.Drawing;

public class MarkerPlugin : BasePlugin
{
    public override string ModuleName => "MarkerPlugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "ThatTamer";

    // Initialize
    private readonly Circle marker = new Circle();
    private MarkerConfig config = new MarkerConfig();
    private static Vector3 ToNumericsVector(Vector v) => new Vector3(v.X, v.Y, v.Z);

    // Load JSON Config
    public override void Load(bool hotReload)
    {
        LoadConfig();
    }

    // Load the Config file.
    private void LoadConfig()
    {
        string configPath = Path.Combine(ModuleDirectory, "marker_config.json");

        // Initial Setup
        if (!File.Exists(configPath))
        {
            // Create new Config File
            string defaultJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, defaultJson);
            Console.WriteLine($"[MarkerPlugin] Created default config at: {configPath}");
        }

        // Config Initilization
        try
        {
            // Read already existing Config File
            string json = File.ReadAllText(configPath);
            MarkerConfig? loadedConfig = JsonSerializer.Deserialize<MarkerConfig>(json);
            if (loadedConfig != null)
            {
                config = loadedConfig;
                Console.WriteLine("[MarkerPlugin] Config loaded successfully.");
            }
        }
        // Error -- Debug in console
        catch (Exception e)
        {
            Console.WriteLine($"[MarkerPlugin] Failed to load config: {e.Message}");
        }
    }

    // Marker Command.
    [ConsoleCommand("marker")]
    public unsafe void OnMarkerCommand(CCSPlayerController? player, CommandInfo info)
    {
        // Validate user
        if (player?.PlayerPawn.Value is not { } pawn)
            return;

        // Check if player is a CT
        if (pawn.TeamNum != 3)
        {
            return;
        }

        // Get the origin point from the player's eye position
        Vector origin = pawn.GetEyePosition();

        // Where is the player looking?
        QAngle viewAngles = pawn.EyeAngles;

        // Conversion: Degrees > Radians
        float pitch = viewAngles.X * (float)(Math.PI / 180.0);
        float yaw = viewAngles.Y * (float)(Math.PI / 180.0);

        // Get view angles
        float cp = (float)Math.Cos(pitch);
        float sp = (float)Math.Sin(pitch);
        float cy = (float)Math.Cos(yaw);
        float sy = (float)Math.Sin(yaw);

        // Computer view angles
        Vector forward = new Vector(cp * cy, cp * sy, -sp);

        // Sets up a raytrace from players eye position to end point -- Projects 4000 units forward to place on far surfaces ( Can Change here. )
        Vector traceStart = origin;
        Vector traceEnd = traceStart + Vec.Scale(forward, 4000f);

        // Set bounding box for the raytrace
        Ray ray = new Ray(ToNumericsVector(new Vector(-16, -16, 0)), ToNumericsVector(new Vector(16, 16, 72)));

        // Configures collision filter for world geometry
        CTraceFilter filter = new CTraceFilter(pawn.Index)
        {
            m_nObjectSetMask = 0xf,
            m_nCollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PLAYER_MOVEMENT,
            m_nInteractsWith = pawn.GetInteractsWith(),
            m_nInteractsExclude = 0,
            m_nBits = 11,
            m_bIterateEntities = true,
            m_nInteractsAs = 0x40000
        };

        // Filter entities
        filter.m_nHierarchyIds[0] = pawn.GetHierarchyId();
        filter.m_nHierarchyIds[1] = 0;

        // What is the player aiming at?
        CGameTrace trace = TraceRay.TraceHull(traceStart, traceEnd, filter, ray);

        // Select color from config. ( Default is Cyan if error or not selected otherwise. )
        Color selectedColor = Lib.COLOUR_CONFIG_MAP.TryGetValue(config.Color, out var c) ? c : Lib.CYAN;

        // Place a marker at this position
        if (trace.Fraction < 1.0f)
        {
            // Destory previous marker.
            marker.Destroy();
            // Set marker color.
            marker.colour = selectedColor;
            // Draw marker.
            marker.Draw(config.Duration, config.Radius, new Vector(trace.EndPos.X, trace.EndPos.Y, trace.EndPos.Z));
        }

    }

    // Remove Marker
    [ConsoleCommand("rmarker")]
    [ConsoleCommand("removemarker")]
    public unsafe void OnRemoveMarkerCommand(CCSPlayerController? player, CommandInfo info)
    {
        // Validate User
        if (player?.PlayerPawn.Value is not { } pawn)
            return;

        // Check if player is a CT
        if (pawn.TeamNum != 3)
        {
            return;
        }

        // Remove marker
        marker.Destroy();

    }

}

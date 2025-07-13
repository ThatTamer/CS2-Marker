using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

public static class Lib
{
    public static readonly Color CYAN = Color.FromArgb(255, 153, 255, 255);
    public static readonly Color RED = Color.FromArgb(255, 255, 0, 0);
    public static readonly Color INVIS = Color.FromArgb(0, 255, 255, 255);
    public static readonly Color GREEN = Color.FromArgb(255, 0, 191, 0);

    public static readonly Dictionary<string, Color> COLOUR_CONFIG_MAP = new Dictionary<string, Color>()
    {
        {"Cyan",Lib.CYAN}, // cyan
        {"Pink",Color.FromArgb(255,255,192,203)} , // pink
        {"Red",Lib.RED}, // red
        {"Purple",Color.FromArgb(255,118, 9, 186)}, // purple
        {"Grey",Color.FromArgb(255,66, 66, 66)}, // grey
        {"Green",GREEN}, // green
        {"Yellow",Color.FromArgb(255,255, 255, 0)} // yellow
    };

    public static readonly Vector VEC_ZERO = new Vector(0.0f, 0.0f, 0.0f);
    public static readonly QAngle ANGLE_ZERO = new QAngle(0.0f, 0.0f, 0.0f);

    static ConVar? blockCvar = ConVar.Find("mp_solid_teammates");
    static ConVar? ff = ConVar.Find("mp_teammates_are_enemies");

    public const int HITGROUP_HEAD = 0x1;
}

public static class Entity
{
    static public void Remove(int index, String name)
    {
        CBaseEntity? ent = Utilities.GetEntityFromIndex<CBaseEntity>(index);

        if (ent != null && ent.DesignerName == name)
        {
            ent.Remove();
        }
    }

    static void ForceEntInput(String name, String input)
    {
        // search for door entitys and open all of them!
        var target = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(name);

        foreach (var ent in target)
        {
            if (!ent.IsValid)
            {
                continue;
            }

            ent.AcceptInput(input);
        }
    }

    // TODO: is their a cheaper way to do this?
    static public int EntCount()
    {
        return Utilities.GetAllEntities().Count();
    }


    static public void Move(this CEnvBeam? laser, Vector start, Vector end)
    {
        if (laser == null)
        {
            return;
        }

        // set pos
        laser.Teleport(start, Lib.ANGLE_ZERO, Lib.VEC_ZERO);

        // end pos
        // NOTE: we cant just move the whole vec
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;

        Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");
    }

    static public void MoveLaserByIndex(int laserIndex, Vector start, Vector end)
    {
        CEnvBeam? laser = Utilities.GetEntityFromIndex<CEnvBeam>(laserIndex);
        if (laser != null && laser.DesignerName == "env_beam")
        {
            laser.Move(start, end);
        }
    }

    static public void SetColour(this CEnvBeam? laser, Color colour)
    {
        if (laser != null)
        {
            laser.Render = colour;
        }
    }


    static public int DrawLaser(Vector start, Vector end, float width, Color colour)
    {
        CEnvBeam? laser = Utilities.CreateEntityByName<CEnvBeam>("env_beam");

        if (laser == null)
        {
            return -1;
        }

        // setup looks
        laser.SetColour(colour);
        laser.Width = 2.0f;

        // circle not working?
        //laser.Flags |= 8;

        laser.Move(start, end);

        // start spawn
        laser.DispatchSpawn();

        return (int)laser.Index;
    }

    public static String DOOR_PREFIX = $" {ChatColors.Green}[Door control]: {ChatColors.White}";

    


    static CCSPlayerController? PlayerFromPawn(CCSPlayerPawn? pawn)
    {
        // pawn valid
        if (pawn == null || !pawn.IsValid)
        {
            return null;
        }

        // controller valid
        if (pawn.OriginalController == null || !pawn.OriginalController.IsValid)
        {
            return null;
        }

        // any further validity is up to the caller
        return pawn.OriginalController.Value;
    }

    static public CCSPlayerController? Player(this CBaseEntity? ent)
    {
        if (ent != null && ent.DesignerName == "player")
        {
            var pawn = new CCSPlayerPawn(ent.Handle);

            return PlayerFromPawn(pawn);
        }

        return null;
    }

    static public CCSPlayerController? Player(this CHandle<CBaseEntity> handle)
    {
        if (handle.IsValid)
        {
            return handle.Value.Player();
        }

        return null;
    }
}
static public class Vec
{
    // TODO: should we have versions not in place?
    static public Vector Scale(Vector vec, float t)
    {
        return new Vector(vec.X * t, vec.Y * t, vec.Z * t);
    }

    static public Vector Add(Vector v1, Vector v2)
    {
        return new Vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    static public Vector Sub(Vector v1, Vector v2)
    {
        return new Vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
    }

    static public Vector Normalize(Vector v1)
    {
        float length = (float)Math.Sqrt((v1.X * v1.X) + (v1.Y * v1.Y) + (v1.Z * v1.Z));

        return new Vector(v1.X / length, v1.Y / length, v1.Z / length);
    }
}

class Line
{
    public void Move(Vector start, Vector end)
    {
        if (laserIndex == -1)
        {
            laserIndex = Entity.DrawLaser(start, end, 2.0f, colour);
        }

        else
        {
            Entity.MoveLaserByIndex(laserIndex, start, end);
        }
    }

    public void Destroy()
    {
        if (laserIndex != -1)
        {
            Entity.Remove(laserIndex, "env_beam");
            laserIndex = -1;
        }
    }

    int laserIndex = -1;
    public Color colour = Lib.CYAN;
}


class Circle
{
    public Circle()
    {
        for (int l = 0; l < lines.Count(); l++)
        {
            lines[l] = new Line();
        }
    }

    static Vector AngleOnCircle(float angle, float r, Vector mid)
    {
        // {r * cos(x),r * sin(x)} + mid
        // NOTE: we offset Z so it doesn't clip into the ground
        return new Vector((float)(mid.X + (r * Math.Cos(angle))), (float)(mid.Y + (r * Math.Sin(angle))), mid.Z + 6.0f);
    }

    public void Draw(float life, float radius, float X, float Y, float Z)
    {
        Vector mid = new Vector(X, Y, Z);

        // draw piecewise approx by stepping angle
        // and joining points with a dot to dot
        float step = (float)(2.0f * Math.PI) / (float)lines.Count();

        float angleOld = 0.0f;
        float angleCur = step;

        for (int l = 0; l < lines.Count(); l++)
        {
            Vector start = AngleOnCircle(angleOld, radius, mid);
            Vector end = AngleOnCircle(angleCur, radius, mid);

            // update the line colour
            lines[l].colour = colour;

            lines[l].Move(start, end);

            angleOld = angleCur;
            angleCur += step;
        }
    }

    public void Draw(float life, float radius, Vector vec)
    {
        Draw(life, radius, vec.X, vec.Y, vec.Z);
    }

    public void Destroy()
    {
        for (int l = 0; l < lines.Count(); l++)
        {
            lines[l].Destroy();
        }
    }

    Line[] lines = new Line[50];
    public Color colour = Lib.CYAN;
}
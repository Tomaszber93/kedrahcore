﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tibia.Objects;
using Tibia.Constants;
using System.Threading;

namespace Kedrah.Modules {
    public class Cavebot : Module {
        #region Variables/Objects

        private int Iterator = 0;
        public List<Waypoint> Waypoints = new List<Waypoint>();
        public int SkipNodes = 2;
        public Item Pick = Tibia.Constants.Items.Tool.Pick;
        public Item Rope = Tibia.Constants.Items.Tool.Rope;
        public Item Shovel = Tibia.Constants.Items.Tool.Shovel;
        public List<Item> LootBodies = new List<Item>();

        #endregion

        #region Constructor/Destructor

        public Cavebot(ref Core core)
            : base(ref core) {

            #region Timers

            Timers.Add("walk", new Tibia.Util.Timer(500, false));
            Timers["walk"].Execute += new Tibia.Util.Timer.TimerExecution(Walk_OnExecute);

            #endregion
        }

        #endregion

        #region Get/Set Objects

        public bool Walk {
            get {
                if (Timers["walk"].State == Tibia.Util.TimerState.Running)
                    return true;
                else
                    return false;
            }
            set {
                if (value)
                    PlayTimer("walk");
                else
                    PauseTimer("walk");
            }
        }

        #endregion

        #region Module Functions

        private Location MovableDirection() {
            List<Tile> tiles = new List<Tile>();
            tiles.Add(Kedrah.Map.GetTile(Kedrah.Player.Location.Offset(1, 0, 0)));
            tiles.Add(Kedrah.Map.GetTile(Kedrah.Player.Location.Offset(-1, 0, 0)));
            tiles.Add(Kedrah.Map.GetTile(Kedrah.Player.Location.Offset(0, 1, 0)));
            tiles.Add(Kedrah.Map.GetTile(Kedrah.Player.Location.Offset(0, -1, 0)));

            foreach (Tile tile in tiles)
                if (!tile.Ground.GetFlag(Tibia.Addresses.DatItem.Flag.BlocksPath) && !tile.Ground.GetFlag(Tibia.Addresses.DatItem.Flag.Blocking) && !tile.Ground.GetFlag(Tibia.Addresses.DatItem.Flag.Floorchange))
                    return tile.Location;

            return Kedrah.Player.Location.Offset(1, 1, 0);
        }

        public bool Reachable(Tibia.Objects.Location location) {
            IEnumerable<Tibia.Objects.Tile> tileList = Kedrah.Map.GetTilesOnSameFloor();
            Tibia.Objects.Tile playerTile = Kedrah.Map.GetTileWithPlayer();
            Tibia.Objects.Tile destinationTile = Kedrah.Map.GetTile(location);

            if (playerTile == null || destinationTile == null)
                return false;
            else if (location.IsAdjacentTo(Kedrah.Player.Location))
                return true;

            IEnumerable<Creature> creatures = Kedrah.Client.BattleList.GetCreatures().Where(c => c.Z == Kedrah.Player.Z);

            foreach (Tibia.Objects.Tile tile in tileList) {
                if (tile.Ground.GetFlag(Tibia.Addresses.DatItem.Flag.Blocking) || tile.Ground.GetFlag(Tibia.Addresses.DatItem.Flag.BlocksPath) ||
                    tile.Items.Any(i => i.GetFlag(Tibia.Addresses.DatItem.Flag.Blocking) || i.GetFlag(Tibia.Addresses.DatItem.Flag.BlocksPath) ||
                    Kedrah.Client.PathFinder.ModifiedItems.ContainsKey(i.Id)) ||
                    creatures.Any(c => tile.Objects.Any(o => o.Data == c.Id && o.Data != Kedrah.Player.Id))) {
                    Kedrah.Client.PathFinder.Grid[tile.MemoryLocation.X, tile.MemoryLocation.Y] = 0;
                }
                else {
                    Kedrah.Client.PathFinder.Grid[tile.MemoryLocation.X, tile.MemoryLocation.Y] = 1;
                }
            }

            return Kedrah.Client.PathFinder.FindPath(playerTile.MemoryLocation, destinationTile.MemoryLocation);
        }

        #endregion

        #region Timers

        public void Walk_OnExecute() {
            if (Kedrah.Player.Target_ID != 0)
                return;

            if (LootBodies.Count > 0) {
                LootBodies.RemoveAll(delegate(Item i) { return !Reachable(i.Location.GroundLocation); });
                LootBodies.Sort(new Comparison<Item>(delegate(Item i1, Item i2) { return i1.Location.GroundLocation.DistanceTo(Kedrah.Player.Location).CompareTo(i2.Location.GroundLocation.DistanceTo(Kedrah.Player.Location)); }));

                if (!LootBodies[0].Location.GroundLocation.IsAdjacentTo(Kedrah.Player.Location) && !Kedrah.Player.IsWalking) {
                    Location location = LootBodies[0].Location.GroundLocation;

                    foreach (Tile t in Kedrah.Map.GetTilesOnSameFloor())
                        if (t.Location.IsAdjacentTo(LootBodies[0].Location.GroundLocation) && t.Location.DistanceTo(Kedrah.Player.Location) < location.DistanceTo(Kedrah.Player.Location))
                            location = t.Location;

                    Kedrah.Player.GoTo = location;
                }
                else if (!Kedrah.Player.IsWalking) {
                    LootBodies[0].OpenAsContainer((byte)Kedrah.Inventory.GetContainers().Count());
                    LootBodies.RemoveAt(0);
                }

                return;
            }

            if (Waypoints.Count <= 0)
                return;

            Waypoint waypoint = Waypoints[Iterator];

            if (!Reachable(waypoint.Location))
                Iterator++;
            else {

                if (!Kedrah.Player.IsWalking && waypoint.Type != WaypointType.Action)
                    Kedrah.Player.GoTo = waypoint.Location;

                switch (waypoint.Type) {
                    case WaypointType.Action:
                        Iterator++;
                        break;
                    case WaypointType.Approach:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) <= 1)
                            Iterator++;
                        break;
                    case WaypointType.Ladder:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) <= 1) {
                            Kedrah.Map.GetTile(waypoint.Location).Ground.Use();
                            Iterator++;
                        }
                        break;
                    case WaypointType.Node:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) <= SkipNodes)
                            Iterator++;
                        break;
                    case WaypointType.Pick:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) == 1) {
                            Pick.Use(Kedrah.Map.GetTile(waypoint.Location));
                            Iterator++;
                        }
                        else if (waypoint.Location == Kedrah.Player.Location)
                            Kedrah.Player.GoTo = MovableDirection();
                        break;
                    case WaypointType.Rope:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) <= 1) {
                            Rope.Use(Kedrah.Map.GetTile(waypoint.Location));
                            Iterator++;
                        }
                        break;
                    case WaypointType.Shovel:
                        if (waypoint.Location.DistanceTo(Kedrah.Player.Location) == 1) {
                            Shovel.Use(Kedrah.Map.GetTile(waypoint.Location));
                            Iterator++;
                        }
                        else if (waypoint.Location == Kedrah.Player.Location)
                            Kedrah.Player.GoTo = MovableDirection();
                        break;
                    case WaypointType.Stand:
                        if (waypoint.Location == Kedrah.Player.Location)
                            Iterator++;
                        break;
                    case WaypointType.Walk:
                        Iterator++;
                        break;
                }

                if (Iterator >= Waypoints.Count)
                    Iterator = 0;
            }
        }

        #endregion
    }

    public enum WaypointType {
        Action,
        Approach,
        Ladder,
        Node,
        Pick,
        Rope,
        Shovel,
        Stand,
        Walk
    }

    public class Waypoint : IComparable<Waypoint> {
        public Core Kedrah;
        public Location Location;
        public WaypointType Type;

        public Waypoint() { }

        public Waypoint(Location location, WaypointType type, Core kedrah) {
            Kedrah = kedrah;
            Location = location;
            Type = type;
        }

        public int CompareTo(Waypoint other) {
            int comparisson = other.Location.DistanceTo(Kedrah.Player.Location).CompareTo(other.Location.DistanceTo(Kedrah.Player.Location));

            return comparisson;
        }

        public override string ToString() {
            return Type.ToString() + ": " + Location.ToString();
        }
    }
}

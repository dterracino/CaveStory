﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CaveStory
{
    public abstract class MapCollidable
    {
        public enum CollisionType
        {
            BouncingCollision,
            StickyCollision
        }

        public List<Tile2D> debugCollidingTiles;
        public List<Tile2D> debugOppositeCollidingTiles;
        CollisionType collisionType;

        public enum AxisType
        {
            XAxis,
            YAxis
        }

        public MapCollidable(CollisionType collisionType)
        {
            debugCollidingTiles = new List<Tile2D>();
            debugOppositeCollidingTiles = new List<Tile2D>();
            this.collisionType = collisionType;
        }

        CollisionInfo? TestMapStickyCollision(Map map, Rectangle rectangle,
            TileInfo.SideType direction, BitArray maybeGroundTile)
        {
            List<CollisionTile> tiles = map.GetCollidingTiles(rectangle, direction);
            for (int i = 0; i < tiles.Count; i++)
            {
                TileInfo.SideType side = TileInfo.OppositeSide(direction);
                GameUnit perpendicularPosition;
                if (TileInfo.Vertical(side))
                {
                    perpendicularPosition = rectangle.Center.X;
                }
                else
                {
                    perpendicularPosition = rectangle.Center.Y;
                }
                GameUnit leadingPosition = rectangle.Side(direction);
                bool shouldTestSlopes = TileInfo.Vertical(side);
                TestCollisionInfo testInfo = tiles[i].TestCollision(side, perpendicularPosition,
                    leadingPosition, shouldTestSlopes);
                if (testInfo.isColliding)
                {
                    CollisionInfo info = new CollisionInfo(testInfo.position, tiles[i].Position, tiles[i].TileType);
                    return info;
                }
                else if (maybeGroundTile != null && direction == TileInfo.SideType.BottomSide)
                {
                    BitArray tallSlope = TileInfo.CreateTileType();
                    tallSlope.Set((int)TileInfo.TileFlag.Slope, true);
                    tallSlope.Set((int)TileInfo.TileFlag.TallSlope, true);
                    if ((maybeGroundTile[(int)TileInfo.TileFlag.Slope] && tiles[i].TileType[(int)TileInfo.TileFlag.Slope]) ||
                        (maybeGroundTile[(int)TileInfo.TileFlag.Wall] &&
                        (tallSlope.And(tiles[i].TileType).Equals(tallSlope))))
                    {
                        CollisionInfo info = new CollisionInfo(testInfo.position, tiles[i].Position, tiles[i].TileType);
                        return info;
                    }
                }
            }
            return null;
        }

        CollisionInfo? TestMapBouncingCollision(Map map, Rectangle rectangle,
            TileInfo.SideType direction, BitArray maybeGroundTile)
        {
            List<CollisionTile> tiles = map.GetCollidingTiles(rectangle, direction);
            CollisionInfo? result = null;
            for (int i = 0; i < tiles.Count; i++)
            {
                TileInfo.SideType side = TileInfo.OppositeSide(direction);
                GameUnit perpendicularPosition;
                if (TileInfo.Vertical(side))
                {
                    perpendicularPosition = rectangle.Center.X;
                }
                else
                {
                    perpendicularPosition = rectangle.Center.Y;
                }
                GameUnit leadingPosition = rectangle.Side(direction);
                bool shouldTestSlopes = TileInfo.Vertical(side);
                TestCollisionInfo testInfo = tiles[i].TestCollision(side, perpendicularPosition,
                    leadingPosition, shouldTestSlopes);
                if (testInfo.isColliding)
                {
                    bool shouldReplaceResult = true;
                    if (result.HasValue)
                    {
                        shouldReplaceResult = TileInfo.IsMin(side) ?
                            testInfo.position < result.Value.position :
                            testInfo.position > result.Value.position;
                    }
                    if (shouldReplaceResult)
                    {
                        CollisionInfo info = new CollisionInfo(testInfo.position, tiles[i].Position, tiles[i].TileType);
                        result = info;
                    }
                }
            }
            return result;
        }

        private void Update(ICollisionRectangle collisionRectangle,
            IAccelerator accelerator,
            Kinematics kinematicsX, Kinematics kinematicsY,
            GameTime gameTime, Map map,
            BitArray maybeGroundTile,
            Kinematics kinematics, AxisType axis)
        {
            Func<Map, Rectangle, TileInfo.SideType, BitArray, CollisionInfo?> testMapCollisionFunc;
            if (collisionType == CollisionType.BouncingCollision)
            {
                testMapCollisionFunc = TestMapBouncingCollision;
            }
            else
            {
                testMapCollisionFunc = TestMapStickyCollision;
            }

            accelerator.UpdateVelocity(kinematics, gameTime);
            GameUnit delta = kinematics.velocity * (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            TileInfo.SideType direction = axis == AxisType.XAxis ?
                (delta > 0 ? TileInfo.SideType.RightSide : TileInfo.SideType.LeftSide) :
                (delta > 0 ? TileInfo.SideType.BottomSide : TileInfo.SideType.TopSide);
            CollisionInfo? maybeInfo = testMapCollisionFunc(map,
                collisionRectangle.Collision(direction, kinematicsX.position, kinematicsY.position, delta),
                direction, maybeGroundTile);

            if (maybeInfo.HasValue)
            {
                kinematics.position = maybeInfo.Value.position - collisionRectangle.BoundingBox.Side(direction);
                debugCollidingTiles.Add(maybeInfo.Value.tilePosition);
                OnCollision(direction, true, maybeInfo.Value.tileType);
            }
            else
            {
                kinematics.position += delta;
                OnDelta(direction);
            }

            maybeInfo = null;
            TileInfo.SideType oppositeDirection = TileInfo.OppositeSide(direction);
            maybeInfo = testMapCollisionFunc(map,
                collisionRectangle.Collision(oppositeDirection, kinematicsX.position, kinematicsY.position, 0),
                oppositeDirection, null);

            if (maybeInfo.HasValue)
            {
                kinematics.position = maybeInfo.Value.position - collisionRectangle.BoundingBox.Side(oppositeDirection);
                debugOppositeCollidingTiles.Add(maybeInfo.Value.tilePosition);
                OnCollision(oppositeDirection, false, maybeInfo.Value.tileType);
            }
        }

        protected void UpdateX(ICollisionRectangle collisionRectangle,
            IAccelerator accelerator,
            Kinematics kinematicsX, Kinematics kinematicsY,
            GameTime gameTime, Map map)
        {
            Update(collisionRectangle, accelerator,
                kinematicsX, kinematicsY,
                gameTime, map,
                null,
                kinematicsX, AxisType.XAxis);
        }

        protected void UpdateY(ICollisionRectangle collisionRectangle,
            IAccelerator accelerator,
            Kinematics kinematicsX, Kinematics kinematicsY,
            GameTime gameTime, Map map,
            BitArray maybeGroundTile)
        {
            Update(collisionRectangle, accelerator,
                kinematicsX, kinematicsY,
                gameTime, map,
                maybeGroundTile,
                kinematicsY, AxisType.YAxis);
        }

        protected abstract void OnCollision(TileInfo.SideType side, bool isDeltaDirection, BitArray tileType);

        protected abstract void OnDelta(TileInfo.SideType side);
    }
}

﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaveStory
{
    public class Player
    {
        // Walk Motion
        // (pixels / millisecond) / millisecond
        static AccelerationUnit Friction { get { return 0.00049804687f; } }
        // (pixels / millisecond) / millisecond
        static AccelerationUnit WalkingAcceleration { get { return 0.00083007812f; } }
        // pixels / millisecond
        static VelocityUnit MaxSpeedX { get { return 0.15859375f; } }

        // Fall Motion
        // (pixels / millisecond) / millisecond
        static AccelerationUnit Gravity { get { return 0.00078125f; } }
        // pixels / millisecond
        static VelocityUnit MaxSpeedY { get { return 0.2998046875f; } }

        // Jump Motion
        // pixels / millisecond
        static VelocityUnit JumpSpeed { get { return 0.25f; } }
        // (pixels / millisecond) / millisecond
        static AccelerationUnit AirAcceleration { get { return 0.0003125f; } }
        // (pixels / millisecond) / millisecond
        static AccelerationUnit JumpGravity { get { return 0.0003125f; } }

        // Sprites
        const string SpriteFilePath = "MyChar";

        // Sprite Frames
        static FrameUnit CharacterFrame { get { return 0; } }

        static FrameUnit WalkFrame { get { return 0; } }
        static FrameUnit StandFrame { get { return 0; } }
        static FrameUnit JumpFrame { get { return 1; } }
        static FrameUnit FallFrame { get { return 2; } }
        static FrameUnit UpFrameOffset { get { return 3; } }
        static FrameUnit DownFrame { get { return 6; } }
        static FrameUnit BackFrame { get { return 7; } }

        // Walk Animation
        static FrameUnit NumWalkFrames { get { return 3; } }
        static int WalkFps { get { return 15; } }

        // Collision Rectangle
        Rectangle CollisionX
        {
            get
            {
                return new Rectangle(6, 10, 20, 12);
            }
        }

        Rectangle CollisionY
        {
            get
            {
                return new Rectangle(10, 2, 12, 30);
            }
        }

        GameUnit x;
        public GameUnit CenterX
        {
            get
            {
                return x + Units.TileToGame(1) / 2.0f;
            }
        }
        GameUnit y;
        VelocityUnit velocityX;
        VelocityUnit velocityY;
        int accelerationX;
        SpriteState.HorizontalFacing horizontalFacing;
        SpriteState.VerticalFacing verticalFacing;
        private bool onGround;
        bool OnGround
        {
            get
            {
                return onGround;
            }
            set
            {
                onGround = value;
            }
        }
        private bool jumpActive;
        bool interacting;

        Dictionary<SpriteState, Sprite> sprites;

        public SpriteState SpriteState
        {
            get
            {
                SpriteState.MotionType motion;
                if (interacting)
                {
                    motion = SpriteState.MotionType.Interacting;
                }
                else if (OnGround)
                {
                    motion = accelerationX == 0 ? SpriteState.MotionType.Standing : SpriteState.MotionType.Walking;
                }
                else
                {
                    motion = velocityY < 0.0f ? SpriteState.MotionType.Jumping : SpriteState.MotionType.Falling;
                }
                return new SpriteState(motion, horizontalFacing, verticalFacing);
            }
        }

        public Player(ContentManager Content, GameUnit x, GameUnit y)
        {
            sprites = new Dictionary<SpriteState, Sprite>();
            InitializeSprites(Content);
            this.x = x;
            this.y = y;
            velocityX = 0;
            velocityY = 0;
            accelerationX = 0;
            horizontalFacing = SpriteState.HorizontalFacing.Left;
            verticalFacing = SpriteState.VerticalFacing.Horizontal;
            onGround = false;
            jumpActive = false;
            interacting = false;
        }

        public void InitializeSprites(ContentManager Content)
        {
            for (SpriteState.MotionType motionType = SpriteState.MotionType.FirstMotionType;
                motionType < SpriteState.MotionType.LastMotionType;
                ++motionType)
            {
                for (SpriteState.HorizontalFacing horizontalFacing = SpriteState.HorizontalFacing.FirstHorizontalFacing;
                    horizontalFacing < SpriteState.HorizontalFacing.LastHorizontalFacing;
                    ++horizontalFacing)
                {
                    for (SpriteState.VerticalFacing verticalFacing = SpriteState.VerticalFacing.FirstVerticalFacing;
                        verticalFacing < SpriteState.VerticalFacing.LastVerticalFacing;
                        ++verticalFacing)
                    {
                        InitializeSprite(Content, new SpriteState(motionType, horizontalFacing, verticalFacing));
                    }
                }
            }
        }

        public void InitializeSprite(ContentManager Content, SpriteState spriteState)
        {
            TileUnit tileY = spriteState.horizontalFacing == SpriteState.HorizontalFacing.Left ?
                Convert.ToUInt32(CharacterFrame) : Convert.ToUInt32(1 + CharacterFrame);

            TileUnit tileX = 0;
            switch (spriteState.motionType)
            {
                case SpriteState.MotionType.Walking:
                    tileX = Convert.ToUInt32(WalkFrame);
                    break;
                case SpriteState.MotionType.Standing:
                    tileX = Convert.ToUInt32(StandFrame);
                    break;
                case SpriteState.MotionType.Interacting:
                    tileX = Convert.ToUInt32(BackFrame);
                    break;
                case SpriteState.MotionType.Jumping:
                    tileX = Convert.ToUInt32(JumpFrame);
                    break;
                case SpriteState.MotionType.Falling:
                    tileX = Convert.ToUInt32(FallFrame);
                    break;
                case SpriteState.MotionType.LastMotionType:
                    break;
            }

            if (spriteState.verticalFacing == SpriteState.VerticalFacing.Up)
            {
                tileX = tileX + Convert.ToUInt32(UpFrameOffset);
            }

            if (spriteState.motionType == SpriteState.MotionType.Walking)
            {
                sprites.Add(spriteState, new AnimatedSprite(Content, SpriteFilePath,
                    Units.TileToPixel(tileX), Units.TileToPixel(tileY),
                    Units.TileToPixel(1), Units.TileToPixel(1), 
                    WalkFps, NumWalkFrames));
            }
            else
            {
                if (spriteState.verticalFacing == SpriteState.VerticalFacing.Down &&
                    (spriteState.motionType == SpriteState.MotionType.Jumping || spriteState.motionType == SpriteState.MotionType.Falling))
                {
                    tileX = Convert.ToUInt32(DownFrame);
                }
                sprites.Add(spriteState, new Sprite(Content, SpriteFilePath,
                    Units.TileToPixel(tileX), Units.TileToPixel(tileY),
                    Units.TileToPixel(1), Units.TileToPixel(1)));
            }
        }

        public Rectangle LeftCollision(GameUnit delta)
        {
            return new Rectangle((int)Math.Round(x) + CollisionX.Left + (int)Math.Round(delta),
                (int)Math.Round(y) + CollisionX.Top,
                CollisionX.Width / 2 - (int)Math.Round(delta),
                CollisionX.Height);
        }

        public Rectangle RightCollision(GameUnit delta)
        {
            return new Rectangle((int)Math.Round(x) + CollisionX.Left + CollisionX.Width / 2,
                (int)Math.Round(y) + CollisionX.Top,
                CollisionX.Width / 2 + (int)Math.Round(delta),
                CollisionX.Height);
        }

        public Rectangle TopCollision(GameUnit delta)
        {
            return new Rectangle((int)Math.Round(x) + CollisionY.Left,
                (int)Math.Round(y) + CollisionY.Top + (int)Math.Round(delta),
                CollisionY.Width / 2,
                CollisionY.Height / 2 - (int)Math.Round(delta));
        }

        public Rectangle bottomCollision(GameUnit delta)
        {
            return new Rectangle((int)Math.Round(x) + CollisionY.Left,
                (int)Math.Round(y) + CollisionY.Top + CollisionY.Height / 2,
                CollisionY.Width,
                CollisionY.Height / 2 + (int)Math.Round(delta));
        }

        public void Update(GameTime gameTime, Map map)
        {
            sprites[SpriteState].Update(gameTime);

            UpdateX(gameTime, map);
            UpdateY(gameTime, map);
        }

        public void UpdateX(GameTime gameTime, Map map)
        {
            AccelerationUnit accX = 0;
            if (accelerationX < 0)
            {
                accX = OnGround ? -WalkingAcceleration : -AirAcceleration;
            }
            else if (accelerationX > 0)
            {
                accX = OnGround ? WalkingAcceleration : AirAcceleration;
            }
            velocityX += accX * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (accelerationX < 0)
            {
                velocityX = Math.Max(velocityX, -MaxSpeedX);
            }
            else if (accelerationX > 0)
            {
                velocityX = Math.Min(velocityX, MaxSpeedX);
            }
            else if (OnGround)
            {
                velocityX = velocityX > 0.0f ?
                    (float)Math.Max(0.0f, velocityX - Friction * gameTime.ElapsedGameTime.TotalMilliseconds) :
                    (float)Math.Min(0.0f, velocityX + Friction * gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            GameUnit delta = velocityX * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            
            if (delta > 0.0f)
            {
                CollisionInfo info = GetWallCollisionInfo(map, RightCollision(delta));

                if (info.collided)
                {
                    x = Units.TileToGame(info.col) - CollisionX.Right;
                    velocityX = 0;
                }
                else
                {
                    x += delta;
                }

                info = GetWallCollisionInfo(map, LeftCollision(0));

                if (info.collided)
                {
                    x = Units.TileToGame(info.col) + CollisionX.Right;
                }
            }
            else
            {
                CollisionInfo info = GetWallCollisionInfo(map, LeftCollision(delta));

                if (info.collided)
                {
                    x = Units.TileToGame(info.col) + CollisionX.Right;
                    velocityX = 0;
                }
                else
                {
                    x += delta;
                }

                info = GetWallCollisionInfo(map, RightCollision(0));

                if (info.collided)
                {
                    x = Units.TileToGame(info.col) - CollisionX.Right;
                }
            }
        }

        public void UpdateY(GameTime gameTime, Map map)
        {
            AccelerationUnit gravity = jumpActive && velocityY < 0 ?
                JumpGravity : Gravity;
            velocityY = (float)Math.Min(velocityY + gravity * gameTime.ElapsedGameTime.TotalMilliseconds, MaxSpeedY);

            GameUnit delta = velocityY * (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (delta > 0)
            {
                CollisionInfo info = GetWallCollisionInfo(map, bottomCollision(delta));

                if (info.collided)
                {
                    y = Units.TileToGame(info.row) - CollisionY.Bottom;
                    velocityY = 0;
                    OnGround = true;
                }
                else
                {
                    y += delta;
                    OnGround = false;
                }

                info = GetWallCollisionInfo(map, TopCollision(0));

                if (info.collided)
                {
                    y = Units.TileToGame(info.row) + CollisionY.Height;
                }
            }
            else
            {
                CollisionInfo info = GetWallCollisionInfo(map, TopCollision(delta));

                if (info.collided)
                {
                    y = Units.TileToGame(info.row) + CollisionY.Height;
                    velocityY = 0;
                }
                else
                {
                    y += delta;
                    OnGround = false;
                }

                info = GetWallCollisionInfo(map, bottomCollision(0));

                if (info.collided)
                {
                    y = Units.TileToGame(info.row) - CollisionY.Bottom;
                    OnGround = true;
                }
            }
        }

        CollisionInfo GetWallCollisionInfo(Map map, Rectangle rectangle)
        {
            CollisionInfo info = new CollisionInfo(false, 0, 0);
            List<CollisionTile> tiles = map.GetCollidingTiles(rectangle);
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].tileType == Tile.TileType.WallTile)
                {
                    info = new CollisionInfo(true, tiles[i].row, tiles[i].col);
                    break;
                }
            }
            return info;
        }

        public void StartMovingLeft()
        {
            interacting = false;
            accelerationX = -1;
            horizontalFacing = SpriteState.HorizontalFacing.Left;
        }

        public void StartMovingRight()
        {
            interacting = false;
            accelerationX = 1;
            horizontalFacing = SpriteState.HorizontalFacing.Right;
        }

        public void StopMoving()
        {
            accelerationX = 0;
        }

        public void LookUp()
        {
            interacting = false;
            verticalFacing = SpriteState.VerticalFacing.Up;
        }

        public void LookDown()
        {
            if (verticalFacing == SpriteState.VerticalFacing.Down)
            {
                return;
            }
            interacting = OnGround;
            verticalFacing = SpriteState.VerticalFacing.Down;
        }

        public void LookHorizontal()
        {
            verticalFacing = SpriteState.VerticalFacing.Horizontal;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            sprites[SpriteState].Draw(spriteBatch, x, y);
        }

        public void StartJump()
        {
            interacting = false;
            jumpActive = true;
            if (OnGround)
            {
                
                velocityY = -JumpSpeed;
            }
        }

        public void StopJump()
        {
            jumpActive = false;
        }
    }
}

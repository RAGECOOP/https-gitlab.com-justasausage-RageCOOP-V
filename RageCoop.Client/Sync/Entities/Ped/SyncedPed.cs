﻿using GTA;
using GTA.Math;
using GTA.Native;
using LemonUI.Elements;
using RageCoop.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RageCoop.Client
{
    /// <summary>
    /// ?
    /// </summary>
    public partial class SyncedPed : SyncedEntity
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Create a local entity (outgoing sync)
        /// </summary>
        /// <param name="p"></param>
        internal SyncedPed(Ped p)
        {
            ID=EntityPool.RequestNewID();
            p.CanWrithe=false;
            p.IsOnlyDamagedByPlayer=false;
            MainPed=p;
            OwnerID=Main.LocalPlayerID;

            Function.Call(Hash._SET_PED_CAN_PLAY_INJURED_ANIMS, false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);
            // MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableMelee, true);

        }

        /// <summary>
        /// Create an empty character with ID
        /// </summary>
        internal SyncedPed(int id)
        {
            ID=id;
            LastSynced=Main.Ticked;
        }
        #endregion
        internal Blip PedBlip = null;
        internal BlipColor BlipColor = (BlipColor)255;
        internal BlipSprite BlipSprite = 0;
        internal float BlipScale = 1;
        internal int VehicleID
        {
            get => CurrentVehicle?.ID ?? 0;
            set
            {
                if (CurrentVehicle == null || value != CurrentVehicle?.ID)
                {
                    CurrentVehicle=EntityPool.GetVehicleByID(value);
                }
            }
        }
        internal SyncedVehicle CurrentVehicle { get; private set; }
        internal VehicleSeat Seat;
        public bool IsPlayer { get => OwnerID == ID && ID != 0; }
        public Ped MainPed { get; internal set; }
        internal int Health { get; set; }

        internal Vector3 HeadPosition { get; set; }
        internal Vector3 RightFootPosition { get; set; }
        internal Vector3 LeftFootPosition { get; set; }

        internal byte WeaponTint { get; set; }
        internal Vehicle _lastVehicle { get; set; }
        internal int _lastVehicleID { get; set; }
        private bool _lastRagdoll = false;
        private ulong _lastRagdollTime = 0;
        private bool _lastInCover = false;
        private byte[] _lastClothes = null;
        internal byte[] Clothes { get; set; }

        internal float Heading { get; set; }

        
        #region -- VARIABLES --
        public byte Speed { get; set; }
        private bool _lastIsJumping = false;
        internal PedDataFlags Flags;

        internal bool IsAiming => Flags.HasPedFlag(PedDataFlags.IsAiming);
        internal bool IsReloading => Flags.HasPedFlag(PedDataFlags.IsReloading);
        internal bool IsJumping => Flags.HasPedFlag(PedDataFlags.IsJumping);
        internal bool IsRagdoll => Flags.HasPedFlag(PedDataFlags.IsRagdoll);
        internal bool IsOnFire => Flags.HasPedFlag(PedDataFlags.IsOnFire);
        internal bool IsInParachuteFreeFall => Flags.HasPedFlag(PedDataFlags.IsInParachuteFreeFall);
        internal bool IsParachuteOpen => Flags.HasPedFlag(PedDataFlags.IsParachuteOpen);
        internal bool IsOnLadder => Flags.HasPedFlag(PedDataFlags.IsOnLadder);
        internal bool IsVaulting => Flags.HasPedFlag(PedDataFlags.IsVaulting);
        internal bool IsInCover => Flags.HasPedFlag(PedDataFlags.IsInCover);
        internal bool IsInLowCover => Flags.HasPedFlag(PedDataFlags.IsInLowCover);
        internal bool IsInCoverFacingLeft => Flags.HasPedFlag(PedDataFlags.IsInCoverFacingLeft);
        internal bool IsBlindFiring => Flags.HasPedFlag(PedDataFlags.IsBlindFiring);
        internal bool IsInStealthMode => Flags.HasPedFlag(PedDataFlags.IsInStealthMode);
        internal Prop ParachuteProp { get; set; } = null;
        internal uint CurrentWeaponHash { get; set; }
        private Dictionary<uint, bool> _lastWeaponComponents = null;
        internal Dictionary<uint, bool> WeaponComponents { get; set; } = null;
        private int _lastWeaponObj = 0;
        #endregion
        internal Vector3 AimCoords { get; set; }

        private WeaponAsset WeaponAsset { get; set; }

        internal override void Update()
        {
            if (Owner==null) { return; }
            if (IsPlayer)
            {
                RenderNameTag();
            }

            // Check if all data avalible
            if (!IsReady) { return; }

            // Skip update if no new sync message has arrived.
            if (!NeedUpdate) { return; }

            if (MainPed == null || !MainPed.Exists())
            {
                if (!CreateCharacter())
                {
                    return;
                }
            }

            // Need to update state
            if (LastFullSynced>=LastUpdated)
            {
                if (MainPed!=null&& (Model != MainPed.Model.Hash))
                {
                    if (!CreateCharacter())
                    {
                        return;
                    }
                }

                if (((byte)BlipColor==255) && (PedBlip!=null))
                {
                    PedBlip.Delete();
                    PedBlip=null;
                }
                else if (((byte)BlipColor != 255) && PedBlip==null)
                {
                    PedBlip=MainPed.AddBlip();

                    if (IsPlayer)
                    {
                        // Main.Logger.Debug("blip:"+Player.Username);
                        Main.QueueAction(() => { PedBlip.Name=Owner.Username; });
                    }
                    PedBlip.Color=BlipColor;
                    PedBlip.Sprite=BlipSprite;
                    PedBlip.Scale=BlipScale;
                }
                if (PedBlip!=null)
                {
                    if (PedBlip.Color!=BlipColor)
                    {
                        PedBlip.Color=BlipColor;
                    }
                    if (PedBlip.Sprite!=BlipSprite)
                    {
                        PedBlip.Sprite=BlipSprite;
                    }
                }

                if (!Clothes.SequenceEqual(_lastClothes))
                {
                    SetClothes();
                }
            }

            if (MainPed.IsDead)
            {
                if (Health>0)
                {
                    if (IsPlayer)
                    {
                        MainPed.Resurrect();
                    }
                    else
                    {
                        SyncEvents.TriggerPedKilled(this);
                    }
                }
            }
            else if (IsPlayer&&(MainPed.Health != Health))
            {
                MainPed.Health = Health;

                if (Health <= 0 && !MainPed.IsDead)
                {
                    MainPed.IsInvincible = false;
                    MainPed.Kill();
                    return;
                }
            }
            if (Speed>=4)
            {
                DisplayInVehicle();
            }
            else
            {
                if (MainPed.IsInVehicle()) { MainPed.Task.LeaveVehicle(LeaveVehicleFlags.WarpOut); }
                DisplayOnFoot();
            }

            LastUpdated=Main.Ticked;
        }

        private void RenderNameTag()
        {
            if (!Owner.DisplayNameTag || (MainPed==null) || !MainPed.IsVisible || !MainPed.IsInRange(Main.PlayerPosition, 40f))
            {
                return;
            }

            Vector3 targetPos = MainPed.Bones[Bone.IKHead].Position;
            Point toDraw = default;
            if (Util.WorldToScreen(targetPos, ref toDraw))
            {
                toDraw.Y-=100;
                new ScaledText(toDraw, Owner.Username, 0.4f, GTA.UI.Font.ChaletLondon)
                {
                    Outline = true,
                    Alignment = GTA.UI.Alignment.Center,
                    Color=Owner.HasDirectConnection? Color.FromArgb(179, 229, 252) : Color.White,
                }.Draw();
            }
        }

        private bool CreateCharacter()
        {
            if (MainPed != null)
            {
                if (MainPed.Exists())
                {
                    // Main.Logger.Debug($"Removing ped {ID}. Reason:CreateCharacter");
                    MainPed.Kill();
                    MainPed.MarkAsNoLongerNeeded();
                    MainPed.Delete();
                }

                MainPed = null;
            }

            if (PedBlip != null && PedBlip.Exists())
            {
                PedBlip.Delete();
                PedBlip = null;
            }
            if (!Model.IsLoaded)
            {
                Model.Request();
                return false;
            }

            if ((MainPed = Util.CreatePed(Model, Position)) == null)
            {
                return false;
            }

            Model.MarkAsNoLongerNeeded();

            MainPed.BlockPermanentEvents = true;
            MainPed.CanWrithe=false;
            MainPed.CanBeDraggedOutOfVehicle = true;
            MainPed.IsOnlyDamagedByPlayer = false;
            MainPed.RelationshipGroup=Main.SyncedPedsGroup;
            MainPed.IsFireProof=false;
            MainPed.IsExplosionProof=false;

            Function.Call(Hash.SET_PED_DROPS_WEAPONS_WHEN_DEAD, MainPed.Handle, false);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, MainPed.Handle, true);
            Function.Call(Hash.SET_PED_CAN_BE_TARGETTED_BY_PLAYER, MainPed.Handle, Game.Player, true);
            Function.Call(Hash.SET_PED_GET_OUT_UPSIDE_DOWN_VEHICLE, MainPed.Handle, false);
            Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, MainPed.Handle, true, true);
            Function.Call(Hash._SET_PED_CAN_PLAY_INJURED_ANIMS, false);
            Function.Call(Hash.SET_PED_CAN_EVASIVE_DIVE, MainPed.Handle, false);

            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DrownsInWater, false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableExplosionReactions, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_AvoidTearGas, false);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_IgnoreBeingOnFire, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableEvasiveDives, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisablePanicInVehicle, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_BlockNonTemporaryEvents, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableShockingEvents, true);
            MainPed.SetConfigFlag((int)PedConfigFlags.CPED_CONFIG_FLAG_DisableHurt, true);

            SetClothes();

            if (IsPlayer) { MainPed.IsInvincible=true; }
            if (IsInvincible) { MainPed.IsInvincible=true; }

            lock (EntityPool.PedsLock)
            {
                // Add to EntityPool so this Character can be accessed by handle.
                EntityPool.Add(this);
            }

            return true;
        }

        private void SetClothes()
        {
            for (byte i = 0; i < 12; i++)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, MainPed.Handle, i, (int)Clothes[i], (int)Clothes[i+12], (int)Clothes[i+24]);
            }
            _lastClothes = Clothes;
        }


        #region ONFOOT
        private string[] _currentAnimation = new string[2] { "", "" };

        private void DisplayOnFoot()
        {

            MainPed.Task.ClearAll();
            CheckCurrentWeapon();
            if (IsInParachuteFreeFall)
            {
                MainPed.PositionNoOffset = Vector3.Lerp(MainPed.ReadPosition(), Position + Velocity, 0.5f);
                MainPed.Quaternion = Rotation.ToQuaternion();

                if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "skydive@base", "free_idle", 3))
                {
                    Function.Call(Hash.TASK_PLAY_ANIM, MainPed.Handle, LoadAnim("skydive@base"), "free_idle", 8f, 10f, -1, 0, -8f, 1, 1, 1);
                }
                return;
            }

            if (IsParachuteOpen)
            {
                if (ParachuteProp == null)
                {
                    Model model = 1740193300;
                    model.Request(1000);
                    if (model != null)
                    {
                        ParachuteProp = World.CreateProp(model, MainPed.ReadPosition(), MainPed.ReadRotation(), false, false);
                        model.MarkAsNoLongerNeeded();
                        ParachuteProp.IsPositionFrozen = true;
                        ParachuteProp.IsCollisionEnabled = false;

                        ParachuteProp.AttachTo(MainPed.Bones[Bone.SkelSpine2], new Vector3(3.6f, 0f, 0f), new Vector3(0f, 90f, 0f));
                    }
                    MainPed.Task.ClearAllImmediately();
                    MainPed.Task.ClearSecondary();
                }

                MainPed.PositionNoOffset = Vector3.Lerp(MainPed.ReadPosition(), Position + Velocity, 0.5f);
                MainPed.Quaternion = Rotation.ToQuaternion();

                if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "skydive@parachute@first_person", "chute_idle_right", 3))
                {
                    Function.Call(Hash.TASK_PLAY_ANIM, MainPed, LoadAnim("skydive@parachute@first_person"), "chute_idle_right", 8f, 10f, -1, 0, -8f, 1, 1, 1);
                }

                return;
            }
            if (ParachuteProp != null)
            {
                if (ParachuteProp.Exists())
                {
                    ParachuteProp.Delete();
                }
                ParachuteProp = null;
            }

            if (IsOnLadder)
            {
                if (Velocity.Z < 0)
                {
                    string anim = Velocity.Z < -2f ? "slide_climb_down" : "climb_down";
                    if (_currentAnimation[1] != anim)
                    {
                        MainPed.Task.ClearAllImmediately();
                        _currentAnimation[1] = anim;
                    }

                    if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", anim, 3))
                    {
                        MainPed.Task.PlayAnimation("laddersbase", anim, 8f, -1, AnimationFlags.Loop);
                    }
                }
                else
                {
                    if (Math.Abs(Velocity.Z) < 0.5)
                    {
                        if (_currentAnimation[1] != "base_left_hand_up")
                        {
                            MainPed.Task.ClearAllImmediately();
                            _currentAnimation[1] = "base_left_hand_up";
                        }

                        if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", "base_left_hand_up", 3))
                        {
                            MainPed.Task.PlayAnimation("laddersbase", "base_left_hand_up", 8f, -1, AnimationFlags.Loop);
                        }
                    }
                    else
                    {
                        if (_currentAnimation[1] != "climb_up")
                        {
                            MainPed.Task.ClearAllImmediately();
                            _currentAnimation[1] = "climb_up";
                        }

                        if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, MainPed.Handle, "laddersbase", "climb_up", 3))
                        {
                            MainPed.Task.PlayAnimation("laddersbase", "climb_up", 8f, -1, AnimationFlags.Loop);
                        }
                    }
                }

                SmoothTransition();
                return;
            }
            else if (MainPed.IsTaskActive(TaskType.CTaskGoToAndClimbLadder))
            {
                MainPed.Task.ClearAllImmediately();
                _currentAnimation[1] = "";
            }

            if (IsVaulting)
            {
                if (!MainPed.IsVaulting)
                {
                    MainPed.Task.Climb();
                }

                SmoothTransition();

                return;
            }
            if (!IsVaulting && MainPed.IsVaulting)
            {
                MainPed.Task.ClearAllImmediately();
            }

            if (IsOnFire && !MainPed.IsOnFire)
            {
                Function.Call(Hash.START_ENTITY_FIRE, MainPed);
            }
            else if (!IsOnFire && MainPed.IsOnFire)
            {
                Function.Call(Hash.STOP_ENTITY_FIRE, MainPed);
            }

            if (IsJumping)
            {
                if (!_lastIsJumping)
                {
                    _lastIsJumping = true;
                    MainPed.Task.Jump();
                }

                SmoothTransition();
                return;
            }
            _lastIsJumping = false;

            if (IsRagdoll || Health==0)
            {
                if (!MainPed.IsRagdoll)
                {
                    MainPed.Ragdoll();
                }
                SmoothTransition();
                if (!_lastRagdoll)
                {
                    _lastRagdoll = true;
                    _lastRagdollTime=Main.Ticked;
                }
                return;
            }
            else
            {
                if (MainPed.IsRagdoll)
                {
                    if (Speed==0)
                    {
                        MainPed.CancelRagdoll();
                    }
                    else
                    {
                        MainPed.Task.ClearAllImmediately();
                    }
                    return;
                }

                _lastRagdoll = false;
            }

            if (IsReloading)
            {
                if (!MainPed.IsTaskActive(TaskType.CTaskReloadGun))
                {
                    MainPed.Task.ReloadWeapon();
                }
                /*
                if (!_isPlayingAnimation)
                {
                    string[] reloadingAnim = MainPed.GetReloadingAnimation();
                    if (reloadingAnim != null)
                    {
                        _isPlayingAnimation = true;
                        _currentAnimation = reloadingAnim;
                        MainPed.Task.PlayAnimation(_currentAnimation[0], _currentAnimation[1], 8f, -1, AnimationFlags.AllowRotation | AnimationFlags.UpperBodyOnly);
                    }
                }
                */
                SmoothTransition();
            }
            else if (IsInCover)
            {
                if (!_lastInCover)
                {
                    Function.Call(Hash.TASK_STAY_IN_COVER, MainPed.Handle);
                }

                _lastInCover=true;
                if (IsAiming)
                {
                    DisplayAiming();
                    _lastInCover=false;
                }
                else if (MainPed.IsInCover)
                {
                    SmoothTransition();
                }
            }
            else if (_lastInCover)
            {
                MainPed.Task.ClearAllImmediately();
                _lastInCover=false;
            }
            else if (IsAiming)
            {
                DisplayAiming();
            }
            else if (MainPed.IsShooting)
            {
                MainPed.Task.ClearAllImmediately();
            }
            else
            {
                WalkTo();
            }
        }

        #region WEAPON
        private void CheckCurrentWeapon()
        {
            if (!WeaponAsset.IsLoaded) { WeaponAsset.Request(); }
            if (MainPed.Weapons.Current.Hash != (WeaponHash)CurrentWeaponHash || !WeaponComponents.Compare(_lastWeaponComponents))
            {
                if (WeaponAsset!=null) { WeaponAsset.MarkAsNoLongerNeeded(); }
                WeaponAsset=new WeaponAsset(CurrentWeaponHash);
                MainPed.Weapons.RemoveAll();
                _lastWeaponObj = Function.Call<int>(Hash.CREATE_WEAPON_OBJECT, CurrentWeaponHash, -1, Position.X, Position.Y, Position.Z, true, 0, 0);

                if (CurrentWeaponHash != (uint)WeaponHash.Unarmed)
                {
                    if (WeaponComponents != null && WeaponComponents.Count != 0)
                    {
                        foreach (KeyValuePair<uint, bool> comp in WeaponComponents)
                        {
                            if (comp.Value)
                            {
                                Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_WEAPON_OBJECT, _lastWeaponObj, comp.Key);
                            }
                        }
                    }
                    Function.Call(Hash.GIVE_WEAPON_OBJECT_TO_PED, _lastWeaponObj, MainPed.Handle);
                }
                _lastWeaponComponents = WeaponComponents;
            }
            if (Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, MainPed, CurrentWeaponHash)!=WeaponTint)
            {
                Function.Call<int>(Hash.SET_PED_WEAPON_TINT_INDEX, MainPed, CurrentWeaponHash, WeaponTint);
            }
        }

        private void DisplayAiming()
        {
            if (Velocity==default)
            {
                MainPed.Task.AimAt(AimCoords, 1000);
            }
            else
            {
                Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD, MainPed.Handle,
                Position.X+Velocity.X, Position.Y+Velocity.Y, Position.Z+Velocity.Z,
                AimCoords.X, AimCoords.Y, AimCoords.Z, 3f, false, 0x3F000000, 0x40800000, false, 512, false, 0);

            }
            SmoothTransition();
        }
        #endregion

        private bool LastMoving;
        private void WalkTo()
        {
            Function.Call(Hash.SET_PED_STEALTH_MOVEMENT, MainPed, IsInStealthMode, 0);
            Vector3 predictPosition = Position + (Position - MainPed.ReadPosition()) + Velocity * 0.5f;
            float range = predictPosition.DistanceToSquared(MainPed.ReadPosition());

            switch (Speed)
            {
                case 1:
                    if (!MainPed.IsWalking || range > 0.25f)
                    {
                        float nrange = range * 2;
                        if (nrange > 1.0f)
                        {
                            nrange = 1.0f;
                        }

                        MainPed.Task.GoStraightTo(predictPosition);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, nrange);
                    }
                    LastMoving = true;
                    break;
                case 2:
                    if (!MainPed.IsRunning || range > 0.50f)
                    {
                        MainPed.Task.RunTo(predictPosition, true);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                case 3:
                    if (!MainPed.IsSprinting || range > 0.75f)
                    {
                        Function.Call(Hash.TASK_GO_STRAIGHT_TO_COORD, MainPed.Handle, predictPosition.X, predictPosition.Y, predictPosition.Z, 3.0f, -1, 0.0f, 0.0f);
                        Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, MainPed.Handle, 1.49f);
                        Function.Call(Hash.SET_PED_DESIRED_MOVE_BLEND_RATIO, MainPed.Handle, 1.0f);
                    }
                    LastMoving = true;
                    break;
                default:
                    if (LastMoving)
                    {
                        MainPed.Task.StandStill(2000);
                        LastMoving = false;
                    }
                    break;
            }
            SmoothTransition();
        }

        private void SmoothTransition()
        {
            var localRagdoll = MainPed.IsRagdoll;
            var dist = Position.DistanceTo(MainPed.ReadPosition());
            if (dist>3)
            {
                MainPed.PositionNoOffset=Position;
                return;
            }
            if (!(localRagdoll || MainPed.IsDead))
            {
                if (!IsAiming)
                {
                    MainPed.Heading=Heading;
                }
                MainPed.Velocity=Velocity+5*dist*(Position-MainPed.ReadPosition());
            }
            else if (Main.Ticked-_lastRagdollTime<10)
            {
                return;
            }
            else if (IsRagdoll)
            {
                var helper = new GTA.NaturalMotion.ApplyImpulseHelper(MainPed);
                var head = MainPed.Bones[Bone.SkelHead];
                var rightFoot = MainPed.Bones[Bone.SkelRightFoot];
                var leftFoot = MainPed.Bones[Bone.SkelLeftFoot];

                // 20:head, 3:left foot, 6:right foot, 17:right hand, 

                helper.EqualizeAmount = 1;
                helper.PartIndex=20;
                helper.Impulse=20*(HeadPosition-head.Position);
                helper.Start();
                helper.Stop();

                helper.EqualizeAmount = 1;
                helper.PartIndex=6;
                helper.Impulse=20*(RightFootPosition-rightFoot.Position);
                helper.Start();
                helper.Stop();

                helper.EqualizeAmount = 1;
                helper.PartIndex=3;
                helper.Impulse=20*(LeftFootPosition-leftFoot.Position);
                helper.Start();
                helper.Stop();
            }
            else
            {
                MainPed.Velocity=Velocity+5*dist*(Position-MainPed.ReadPosition());
            }
        }




        #endregion
        private void DisplayInVehicle()
        {
            if (CurrentVehicle==null || CurrentVehicle.MainVehicle==null) { return; }
            switch (Speed)
            {
                case 4:
                    if (MainPed.CurrentVehicle!=CurrentVehicle.MainVehicle)
                    {
                        MainPed.SetIntoVehicle(CurrentVehicle.MainVehicle, Seat);
                    }
                    if (MainPed.IsOnTurretSeat())
                    {
                        // Function.Call(Hash.SET_VEHICLE_TURRET_SPEED_THIS_FRAME, MainPed.CurrentVehicle, 100);
                        Function.Call(Hash.TASK_VEHICLE_AIM_AT_COORD, MainPed.Handle, AimCoords.X, AimCoords.Y, AimCoords.Z);
                    }
                    if (MainPed.VehicleWeapon!=(VehicleWeaponHash)CurrentWeaponHash)
                    {
                        MainPed.VehicleWeapon=(VehicleWeaponHash)CurrentWeaponHash;
                    }
                    break;
                case 5:
                    if (MainPed.VehicleTryingToEnter!=CurrentVehicle.MainVehicle || MainPed.GetSeatTryingToEnter()!=Seat)
                    {
                        MainPed.Task.EnterVehicle(CurrentVehicle.MainVehicle,Seat,-1,5,EnterVehicleFlags.AllowJacking);
                    }
                    break;
                case 6:
                    if (!MainPed.IsTaskActive(TaskType.CTaskExitVehicle))
                    {
                        MainPed.Task.LeaveVehicle(CurrentVehicle.Velocity.Length() > 5f ? LeaveVehicleFlags.BailOut:LeaveVehicleFlags.None);
                    }
                    break;
            }



            /*
            Function.Call(Hash.TASK_SWEEP_AIM_ENTITY,P, "random@paparazzi@pap_anims", "sweep_low", "sweep_med", "sweep_high", -1,V, 1.57f, 0.25f);
            Function.Call(Hash.SET_PED_STEALTH_MOVEMENT, P,true, 0);
            return Function.Call<bool>(Hash.GET_PED_STEALTH_MOVEMENT, P);
            */
        }
    }
}